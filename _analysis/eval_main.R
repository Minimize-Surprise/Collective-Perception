source("functions.R")

vm_dir <- "data/2021_08_13_vm_mr_verification/vm"
mr_dir <- "data/2021_08_13_vm_mr_verification/mr"

dirs <- c(vm_dir, mr_dir)

df <- data.frame()

for (path_to_dir in dirs) {
  
  params_path <- list.files(path=path_to_dir, pattern="_Parameters\\.txt$", full.names = T)[[1]]
  params_obj <- params_obj_from_filepath(params_path)
  
  
  positions_opinions_path <- list.files(path=path_to_dir, pattern="_PositionsOpinions\\.csv$", full.names = T)[[1]]
  
  ## Decision Process over time
  df <- rbind(df, prep_opinion_position_data(positions_opinions_path, params_obj$decisionRule))
}

# Aggregate opinion distribution per time
agg_data <- df %>%
  group_by(decision_rule, runID, time) %>%
  summarise(mean_black = mean(opinion_int)*100) %>%
  mutate(time = as.integer(time))

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

# Runs per decision rule
n_runs_per_rule <-  
  agg_data %>%
  select(decision_rule, runID) %>%
  distinct() %>%
  group_by(decision_rule) %>%
  summarise(n_runs = n())

ggplot(agg_data, aes(x=time, y=mean_black, fill=runID, color=runID)) +
  geom_line(alpha=0.7) +
  theme_bw() +
  facet_wrap(decision_rule~., ncol=1) +
  labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
       title="Decision process over time") +
  theme(legend.position = "none") +
  geom_vline(aes(xintercept = mean_consensus), agg_cons_df, colour="darkred", linetype="dashed")
