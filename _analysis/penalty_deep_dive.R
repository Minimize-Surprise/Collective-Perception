source("functions.R")

path_to_dir <- "data/2021_09_07_wrong_consensus_penalty/mutation020/"

params_path <- list.files(path=path_to_dir, pattern="_Parameters\\.txt$", full.names = T)[[1]]
params_obj <- params_obj_from_filepath(params_path)
run_id <- params_obj$runID

arena_settings_path <- list.files(path=path_to_dir, pattern="_arena_settings\\.csv$", full.names = T)[[1]]
arena_settings_df <- read_csv2(arena_settings_path)

evo_hist_path <- list.files(path=path_to_dir, pattern="_evolution_history\\.json$", full.names = T)[[1]]
json_str <- read_file(evo_hist_path)

json_df <- jsonlite::fromJSON(json_str)  

best_fitness_data <- json_df %>% 
  select(generationCounter, bestFitness) %>%
  mutate(generationCounter = as.integer(generationCounter))

all_fitness_data <- json_df %>%
  select(generationCounter, completeFitnessVals, completeFitnessIDs) %>%
  unnest(c(completeFitnessVals, completeFitnessIDs)) %>% 
  mutate(id = str_split(completeFitnessIDs, "\\.") %>% map_chr(., 1))

### Eval Round Deep Dive (sub-fitness value level) ============
eval_round_df <- json_df %>% 
  select(generationCounter, completeFitnessIDs, completeEvalTuples) %>%
  unnest(cols= c(completeFitnessIDs, completeEvalTuples)) %>%
  group_by(generationCounter) %>%
  mutate(popID = row_number()) %>%
  unnest(cols= c(completeEvalTuples)) %>%
  group_by(generationCounter, popID) %>%
  mutate(evalIDs = row_number()) %>%
  mutate(inversed = ifelse(evalIDs %% 2 != 0, F, T)) %>%
  rename(predictionCount = Item1, predictionError = Item2,
         correctOpinionPercentage = Item3) %>%
  mutate(normError = predictionError / predictionCount / 3) %>%
  mutate(normFitness = 1- normError)


eval_round_penalty_df <- eval_round_df %>%
  mutate(penalizedFitness = calc_penalized_fitness(normFitness, correctOpinionPercentage))




max_gen <- 150
ggplot(eval_round_penalty_df %>% filter(generationCounter %in% c(1:3 , (max_gen-2): max_gen)), 
       aes(y = penalizedFitness, colour=as.factor(popID), x = as.factor(popID), 
           shape=inversed)) +
  facet_wrap(generationCounter~., nrow=3, dir="v") +
  geom_point() +
  theme_bw() +
  labs(title="Distribution of single evaluation round outcomes",
       subtitle="First vs. last generations") +
  theme(legend.position="none") +
  labs(caption=run_id)


manual_best_fitness_df <- eval_round_penalty_df %>%
  group_by(generationCounter, popID) %>% 
  slice(which.min(penalizedFitness)) %>%
  group_by(generationCounter) %>%
  slice(which.max(penalizedFitness))
  
ggplot(manual_best_fitness_df, aes(x=generationCounter, y=penalizedFitness)) +
  geom_line(color="red") +
  theme_bw() +
  labs(x = "generation", y = "generation's best fitness",
       title="Best fitness over generations",
       subtitle="Externally calculated",
       caption=run_id) +
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1))

# <-------------------------------------------------------------------->
# _neural_net_log.csv ----------------------------------------------
# <-------------------------------------------------------------------->
if(FALSE) {
  nn_log_path <- list.files(path=path_to_dir, pattern="_neural_net_log\\.csv$", full.names = T)[[1]]
  
  raw_df <- read_csv2(nn_log_path)
  
  # join with generation / fitness data
  df <- raw_df %>%
    left_join(all_fitness_data %>% 
                select(-id) %>%
                rename(fitness = completeFitnessVals), by=c("networkID" = "completeFitnessIDs")) %>%
    left_join(arena_settings_df, by=c("arenaIndex", "run" = "round"))
  
  best_network_ids <- 
    all_fitness_data %>%
    group_by(generationCounter) %>%
    filter(completeFitnessVals == max(completeFitnessVals))
}

# <-------------------------------------------------------------------->
# _PositionsOpinions.csv ----------------------------------------------
# <-------------------------------------------------------------------->

positions_opinions_path <- list.files(path=path_to_dir, pattern="_PositionsOpinions\\.csv$", full.names = T)[[1]]

## Decision Process over time
df <- prep_opinion_position_data(positions_opinions_path, params_obj$decisionRule)

agg_data <- df %>%
  group_by(decision_rule, runID, time) %>%
  summarise(mean_black = mean(opinion_int)*100) %>%
  mutate(time = as.integer(time))

rm("df")
gc()

agg_inverse_data <- agg_data %>%
  left_join(arena_settings_df %>%
              mutate(runID=paste0(round,"_", arenaIndex)),
            by="runID")

consensus_df <-
  agg_data %>%
  filter(mean_black == 100) %>%
  group_by(decision_rule, runID) %>%
  mutate(consensus_time = min(time)) %>%
  select(decision_rule, runID, consensus_time) %>%
  distinct()

agg_cons_df <- consensus_df %>% 
  group_by(decision_rule) %>%
  summarise(mean_consensus = mean(consensus_time), n_consensus=n())

max_gen <- max(agg_inverse_data$generationIndex)
ggplot(agg_inverse_data %>% 
         filter(generationIndex %in% c(0:2, (max_gen - 2):max_gen)), 
       aes(x=time, y=mean_black, fill=runID, color=tilesInversed)) +
  geom_line(alpha=0.7) +
  theme_bw() +
  facet_wrap(.~generationIndex, ncol=3, dir = "h") +
  labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
       title="Decision process over time",
       caption=run_id)

# Runs per decision rule
n_runs_per_rule <-  
  agg_data %>%
  select(decision_rule, runID) %>%
  distinct() %>%
  group_by(decision_rule) %>%
  summarise(n_runs = n())