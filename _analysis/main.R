source("functions.R")
# TODO: Check pred_outputs variable! (just ground sensor or more?)

penalize_fitness_mode <- T # boolean: T or F

dirs <- c(
  #"../../data/2021_08_27_penalty/increased-length/"
  #"../../data/2021_09_16_penalty_fixed/increased-length/",
  #"../../data/2021_09_16_penalty_fixed/gen300/"
  #"../../data/2021_09_20_penalty_postmerge/300-gen/"
  "../../data/2021_09_23_penalty_newtaskdiff/"
  #"../../data/2021_09_16_penalty_fixed/standard/"
  #"../../data/2021_08_27_penalty/run1/"
  #"data/2021_09_07_wrong_consensus_penalty/mutation020/"
  )

plot_evo_tree <- T

plot_eval_rounds_per_xgens <- T
plot_neural_net_io_per_gen <- T
plot_best_nn_io_per_gen <- T
plot_decision_process_summary <- T
plot_decision_process_over_xGens <- T || plot_decision_process_summary

json_import_error_files <- c()

for (path_to_dir in dirs) {
# <-------------------------------------------------------------------->
#  _Parameters.txt --------------------------------------------
# <-------------------------------------------------------------------->
params_path <- list.files(path=path_to_dir, pattern="_Parameters\\.txt$", full.names = T)[[1]]
params_obj <- params_obj_from_filepath(params_path)
run_id <- params_obj$runID

plot_dir_path <- file.path(path_to_dir, paste0(fs::path_sanitize(run_id),"_plots"))
if (!dir.exists(plot_dir_path)) {
  dir.create(plot_dir_path)  
}

# <-------------------------------------------------------------------->
#  _arena_settings.csv --------------------------------------------
# <-------------------------------------------------------------------->
arena_settings_path <- list.files(path=path_to_dir, pattern="_arena_settings\\.csv$", full.names = T)[[1]]
arena_settings_df <- read_csv2(arena_settings_path)


## _evolution_history.json ----------------------------------------------
evo_hist_path <- list.files(path=path_to_dir, pattern="_evolution_history\\.json$", full.names = T)[[1]]

### Fitness Analysis ================================
json_str <- read_file(evo_hist_path)

if (!jsonlite::validate(json_str)) {
  json_df <- jsonlite::fromJSON(paste0(json_str, "]"))
  write_lines("JSON Error importing json evo hist file", file.path(plot_dir_path, "import_log.txt"))
  json_import_error_files <- c(json_import_error_files, path_to_dir)
} else {
  json_df <- jsonlite::fromJSON(json_str)  
}

best_fitness_data <- json_df %>% 
  select(generationCounter, bestFitness) %>%
  mutate(generationCounter = as.integer(generationCounter))

all_fitness_data <- json_df %>%
  select(generationCounter, completeFitnessVals, completeFitnessIDs) %>%
  unnest(c(completeFitnessVals, completeFitnessIDs)) %>% 
  mutate(id = str_split(completeFitnessIDs, "\\.") %>% map_chr(., 1))

# Best Fitness Plot
ggplot(best_fitness_data, aes(x=generationCounter, y=bestFitness)) +
  geom_line(color="red") +
  theme_bw() +
  labs(x = "generation", y = "generation's best fitness",
       title="Best fitness over generations",
       caption=run_id) +
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1))

ggsave(file.path(plot_dir_path, "fitness_best_over_generations.png"),
       width=9, height=5, unit="in")

## All fitness point plot (all red)
ggplot(all_fitness_data, aes(x=generationCounter, y = completeFitnessVals)) +
  geom_point(alpha=0.5, color="red") +
  labs(x = "generation", y = "fitness",
       title="Population fitness over generations",
       caption=run_id) +
  theme_bw() +
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1))

ggsave(file.path(plot_dir_path, "fitness_all_over_generations.png"),
       width=9, height=5, unit="in")

## All fitness point plot (coloured points)
ggplot(all_fitness_data, aes(x=generationCounter, y = completeFitnessVals, colour=id)) +
  geom_point(alpha=0.5) +
  labs(x = "generation", y = "fitness",
       title="Population fitness over generations",
       subtitle="Coloured according to first generation parent",
       caption=run_id) +
  theme_bw() +
  theme(legend.position="none")+
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1))

ggsave(file.path(plot_dir_path, "fitness_all_coloured_over_generations.png"),
       width=9, height=5, unit="in")

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
  mutate(normFitness = 1 - normError) %>% 
  mutate(penalizedFitness = calc_penalized_fitness(normFitness, correctOpinionPercentage))


if (plot_eval_rounds_per_xgens) {
  
  print(">> Start @ plot_eval_rounds_per_xgens")
  
  
  n_include_gens <- 5
  max_gen <- max(eval_round_df$generationCounter)
  eval_round_plot_dir <- file.path(plot_dir_path, "eval_round_plots_per_gen")
  dir.create(eval_round_plot_dir)
  
  for(start_gen in seq(1, max_gen, n_include_gens)) {
    gen_to <- start_gen + n_include_gens - 1
    
    # Point plot: Distribution of eval round values
    ggplot(eval_round_df %>% 
             filter(between(generationCounter, start_gen, gen_to)), 
           aes(x=popID, 
               y = (if (penalize_fitness_mode) penalizedFitness else normFitness),
               colour=as.factor(popID))) +
      facet_wrap(generationCounter~., ncol = 1) +
      geom_point(alpha=0.5) +
      labs(title=paste("Eval values for generations", start_gen, "to", gen_to)) + 
      theme_bw() +
      theme(legend.position="none") +
      labs(caption=run_id)
      
    ggsave(file.path(eval_round_plot_dir, paste0("points_gen_", start_gen, ".png")),
           width=9, height=9, units="in")
      
    
    # Boxplot: Distribution of eval round values
    ggplot(eval_round_df %>% 
             filter(between(generationCounter, start_gen, gen_to)), 
           aes(x=popID, 
               y = (if (penalize_fitness_mode) penalizedFitness else normFitness), 
               colour=as.factor(popID))) +
      facet_wrap(generationCounter~., ncol = 1) +
      geom_boxplot() +
      labs(title=paste("Eval values for generations", start_gen, "to", gen_to)) + 
      theme_bw() +
      theme(legend.position="none") +
      labs(caption=run_id)
      
    ggsave(file.path(eval_round_plot_dir, paste0("boxplot_", start_gen, ".png")),
           width=9, height=9, units="in")
    print(paste("eval_round_plots_per_gen: Generation #", start_gen, "done!"))
  }

  print(">> Finished @ plot_eval_rounds_per_xgens")
  
  
}

# Facetted Boxplot: Distribution of eval round values
# - First 3 vs. last 3 generations
max_gen <- max(eval_round_df$generationCounter)
ggplot(eval_round_df %>% filter(generationCounter %in% c(1:3 , (max_gen-2): max_gen)), 
       aes(y = (if (penalize_fitness_mode) penalizedFitness else normFitness), 
           colour=as.factor(popID))) +
  facet_wrap(generationCounter~., nrow=3, dir="v") +
  geom_boxplot() +
  theme_bw() +
  labs(title="Distribution of single evaluation round outcomes",
       subtitle="First vs. last generations") +
  theme(legend.position="none") +
  labs(caption=run_id)

ggsave(file.path(plot_dir_path, "eval_rounds_first_and_last_gens_boxplot.png"),
       width=9, height=9, units="in")


ggplot(eval_round_df %>% filter(generationCounter %in% c(1:3 , (max_gen-2): max_gen)), 
       aes(y = (if (penalize_fitness_mode) penalizedFitness else normFitness), 
           colour=as.factor(popID), x = as.factor(popID), 
           shape=inversed)) +
  facet_wrap(generationCounter~., nrow=3, dir="v") +
  geom_point() +
  theme_bw() +
  labs(title="Distribution of single evaluation round outcomes",
       subtitle="First vs. last generations") +
  theme(legend.position="none") +
  labs(caption=run_id)

ggsave(file.path(plot_dir_path, "eval_rounds_first_and_last_gens_scatter.png"),
       width=9, height=9, units="in")

### Evolution Tree Hierarchy

if(plot_evo_tree){
  
  print(">> Start @ plot_evo_tree")
  
  

json_str <- read_file(evo_hist_path)

if (!jsonlite::validate(json_str)) {
  json_df <- jsonlite::fromJSON(paste0(json_str, "]"))
  write_lines("JSON Error importing json evo hist file", file.path(plot_dir_path, "import_log.txt"))
} else {
  json_df <- jsonlite::fromJSON(json_str)  
}
edges <- json_df$completeFitnessIDs

edge_list <- gen_edge_df(edges)
edge_df <- data.frame(matrix(unlist(edge_list), nrow=length(edge_list), byrow=T), stringsAsFactors = F)

name <- unique(c(as.character(edge_df$X1), as.character(edge_df$X2)))
vertices_df <- data.frame(
  name=name,
  value=sample(seq(10,30), length(name), replace=T)
) %>%
  mutate(group = str_split(name, "\\.") %>% map_chr(., 1))

my_graph <- graph_from_data_frame(edge_df, vertices = vertices_df)

ggraph(my_graph, layout = 'partition') + # layout="tree" also works okay - but with overlaps
  geom_edge_diagonal()  +
  geom_node_point(aes(color=group), size = 0.65) +
  theme_void() + 
  theme(legend.position = "none")

ggsave(file.path(plot_dir_path, "evolution_tree.png"),
       width=9, height=9, units="in")


print(">> finished @ plot_evo_tree")


}

# <-------------------------------------------------------------------->
# _neural_net_log.csv ----------------------------------------------
# <-------------------------------------------------------------------->
nn_log_path <- list.files(path=path_to_dir, pattern="_neural_net_log\\.csv$", full.names = T)[[1]]

raw_df <- read_csv2(nn_log_path)

# join with generation / fitness data
df <- raw_df %>%
  left_join(all_fitness_data %>% 
                select(-id) %>%
                rename(fitness = completeFitnessVals), by=c("networkID" = "completeFitnessIDs")) %>%
  left_join(arena_settings_df, by=c("arenaIndex", "run" = "round"))



if (plot_neural_net_io_per_gen) {
  
  print(">> Start @ plot_neural_net_io_per_gen")
  
  
  nn_gen_plot_dir <- file.path(plot_dir_path, "nn_gen_plots")
  dir.create(nn_gen_plot_dir)
  
  for(currentGen in (1:max(df$generationCounter))) {
  #for(currentGen in (1:3)) {
    ### Prediction Net
    pred_df <- df %>% 
      filter(generationCounter == currentGen) %>%
      #head %>%
      preprocess_neuralnetlog_df("pred")
    
    long_pred <- pred_df %>%
      pivot_longer(!nnlog_id_cols) 
    
    long_pred %>%
      #filter(arenaIndex %% 2 == 1) %>%
      ggplot(aes(x=value, color=tilesInversed, fill=tilesInversed)) + 
      geom_histogram(position="identity", alpha=0.1, bins=15) +
      facet_wrap(.~name, ncol=4) +
      theme_bw() +
      labs(title="Prediction net data", 
           subtitle=paste0("All nets from generation #", currentGen),
           caption=run_id)
    
    ggsave(file.path(nn_gen_plot_dir, paste0("pred_gen",currentGen,".png")),
           width=9, height=5, units="in")
    
    ### Decision Net
    dec_df <- df %>%
      filter(generationCounter == currentGen) %>%
      #head %>%
      preprocess_neuralnetlog_df("dec")
    
    long_dec <- dec_df %>%
      pivot_longer(!nnlog_id_cols)
    
    long_dec %>%
      ggplot(aes(x=value, color=tilesInversed, fill=tilesInversed)) + 
      geom_histogram(position="identity", alpha=0.1, bins=15) +
      facet_wrap(.~name, ncol=4) +
      theme_bw() +
      labs(title="Decision net data",
           subtitle=paste0("All nets from generation #", currentGen),
           caption=run_id)
    
    ggsave(file.path(nn_gen_plot_dir, paste0("dec_gen",currentGen,".png")),
           width=9, height=5, units="in")
    print(paste0("> nn_gen_plots: Generation #", currentGen, " done!"))
  }
  
  print(">> Start @ plot_neural_net_io_per_gen")
  
}


best_network_ids <- 
  all_fitness_data %>%
    group_by(generationCounter) %>%
    filter(completeFitnessVals == max(completeFitnessVals))
    

if (plot_best_nn_io_per_gen) {
  
  print(">> Start @ plot_best_nn_io_per_gen")
  
  
  nn_gen_best_plot_dir <- file.path(plot_dir_path, "nn_best_of_gen_plots")
  dir.create(nn_gen_best_plot_dir)
  
  for(currentGen in (1:length(best_network_ids$completeFitnessIDs))) {
    current_best_id <- best_network_ids$completeFitnessIDs[currentGen]
    
    #for(currentGen in (1:3)) {
    ### Prediction Net
    pred_df <- df %>% 
      filter(networkID  == current_best_id) %>%
      #head %>%
      preprocess_neuralnetlog_df("pred")
    
    long_pred <- pred_df %>%
      pivot_longer(!nnlog_id_cols) 
    
    long_pred %>%
      #filter(arenaIndex %% 2 == 1) %>%
      ggplot(aes(x=value, color=tilesInversed, fill=tilesInversed)) + 
      geom_histogram(position="identity", alpha=0.1, bins=15) +
      facet_wrap(.~name, ncol=4) +
      theme_bw() +
      labs(title="Prediction net data", 
           subtitle=paste0("Best net from generation #", currentGen),
           caption=run_id)
    
    ggsave(file.path(nn_gen_best_plot_dir, paste0("pred_gen",currentGen,".png")),
           width=9, height=5, units="in")
    
    ### Decision Net
    dec_df <- df %>%
      filter(generationCounter == currentGen) %>%
      #head %>%
      preprocess_neuralnetlog_df("dec")
    
    long_dec <- dec_df %>%
      pivot_longer(!nnlog_id_cols)
    
    long_dec %>%
      ggplot(aes(x=value, color=tilesInversed, fill=tilesInversed)) + 
      geom_histogram(position="identity", alpha=0.1, bins=15) +
      facet_wrap(.~name, ncol=4) +
      theme_bw() +
      labs(title="Decision net data",
           subtitle=paste0("Best net from generation #", currentGen),
           caption=run_id)
    
    ggsave(file.path(nn_gen_best_plot_dir, paste0("dec_gen",currentGen,".png")),
           width=9, height=5, units="in")
    print(paste0("> nn_best_of_gen_plots: Generation #", currentGen, " done!"))
  }
  
  print(">> Finished @ plot_best_nn_io_per_gen")
}

rm("raw_df", "df", "pred_df", "long_pred", "dec_df", "long_dec")
gc()

# <-------------------------------------------------------------------->
# _PositionsOpinions.csv ----------------------------------------------
# <-------------------------------------------------------------------->

if(plot_decision_process_summary || plot_decision_process_over_xGens) {
  print(">> Start @ decision summary df import")
  
  positions_opinions_path <- list.files(path=path_to_dir, pattern="_PositionsOpinions\\.csv$", full.names = T)[[1]]
  df <- fast_prep_opinion_position_data(positions_opinions_path, params_obj$decisionRule)
  
  # Aggregate opinion distribution per time
  agg_data <- df %>%
    group_by(decision_rule, runID, time) %>%
    summarise(mean_black = mean(opinion_int)*100) %>%
    mutate(time = as.integer(time))
  
  rm("df")
  gc()
  
  print(">> Finished @ decision summary df import")
  
}


if(plot_decision_process_summary) {
  print(">> Start @ plot_decision_process_summary")
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

# Runs per decision rule
n_runs_per_rule <-  
  agg_data %>%
  select(decision_rule, runID) %>%
  distinct() %>%
  group_by(decision_rule) %>%
  summarise(n_runs = n())

#ggplot(agg_data, aes(x=time, y=mean_black, fill=runID, color=runID)) +
#  geom_line() +
#  theme_bw() +
#  facet_wrap(decision_rule~., ncol=1) +
#  labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
#       title="Decision process over time",
#       caption=run_id) +
#  theme(legend.position = "none") +
#  geom_vline(aes(xintercept = mean_consensus), agg_cons_df, colour="darkred", linetype="dashed")

  max_gen <- max(agg_inverse_data$generationIndex, na.rm = T)
  ggplot(agg_inverse_data %>% 
           filter(generationIndex %in% c(0:2, (max_gen - 2):max_gen)), 
         aes(x=time, y=mean_black, fill=runID, color=tilesInversed)) +
    geom_line(alpha=0.3) +
    theme_bw() +
    facet_wrap(.~generationIndex, ncol=3, dir = "h") +
    labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
         title="Decision process over time",
         subtitle=paste("Columns: Generations | Rows: Inversed arena | First and last 3 generations"),
         caption=run_id) 
  
  agg_inverse_data <- agg_data %>%
    left_join(arena_settings_df %>%
                mutate(runID=paste0(round,"_", arenaIndex)),
              by="runID")
  
  print(">> finish @ plot_decision_process_summary")
}


if(plot_decision_process_summary) {
  print(">> Start @ plot_decision_process_summary")
  ## Decision Process over time
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
  
  #ggplot(agg_data, aes(x=time, y=mean_black, fill=runID, color=runID)) +
  #  geom_line() +
  #  theme_bw() +
  #  facet_wrap(decision_rule~., ncol=1) +
  #  labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
  #       title="Decision process over time",
  #       caption=run_id) +
  #  theme(legend.position = "none") +
  #  geom_vline(aes(xintercept = mean_consensus), agg_cons_df, colour="darkred", linetype="dashed")

    max_gen <- max(agg_inverse_data$generationIndex, na.rm = T)
    ggplot(agg_inverse_data %>% 
             filter(generationIndex %in% c(0:2, (max_gen - 2):max_gen)), 
           aes(x=time, y=mean_black, fill=runID, color=tilesInversed)) +
      geom_line(alpha=0.3) +
      theme_bw() +
      facet_wrap(.~generationIndex, ncol=3, dir = "h") +
      labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
           title="Decision process over time",
           subtitle=paste("Columns: Generations | Rows: Inversed arena | First and last 3 generations"),
           caption=run_id) 
    
    ggsave(file.path(plot_dir_path, "decision_process_first_last_gens.png"),
           width=9, height=5, units="in")
    
    print(">> Finished @ plot_decision_process_summary")
    
}
  
if(plot_decision_process_over_xGens) {
  print(">> Start @ plot_decision_process_over_xGens")
  
  # Decision process over time
  
    decision_process_plot_dir <- file.path(plot_dir_path, "decision_process")
    dir.create(decision_process_plot_dir)
    
    n_gen_plot <- 4
    for(start_gen in seq(0, max(agg_inverse_data$generationIndex, na.rm = T), n_gen_plot)) {
      end_gen <- start_gen + n_gen_plot - 1
      ggplot(agg_inverse_data %>% 
               filter(between(generationIndex, start_gen, end_gen)), 
             aes(x=time, y=mean_black, fill=runID, color=tilesInversed)) +
        geom_line(alpha=0.5) +
        theme_bw() +
        facet_wrap(.~generationIndex, ncol=n_gen_plot / 2, dir = "h")
        labs(x = "time [sec]", y = "Bots with opinion BLACK [%]",
             title="Decision process over time",
             subtitle=paste("Columns: Generations | Rows: Inversed arena | Generations", start_gen, "-", end_gen),
             caption=run_id) +
        theme(legend.position = "none")  
      
      ggsave(file.path(decision_process_plot_dir, paste0("decision_process_",start_gen,".png")),
             width=9, height=5, units="in")
      print(paste0("> decision_process: Generation #", start_gen, " done!"))
    }
    
    print(">> Finished @ plot_decision_process_over_xGens")
}


# <-------------------------------------------------------------------->
# _initial_settings.json ----------------------------------------------
# <-------------------------------------------------------------------->
init_settings_path <- list.files(path=path_to_dir, pattern="_initial_settings\\.json$", full.names = T)[[1]]


}

print("JSON Import error for these files:")
print(json_import_error_files)