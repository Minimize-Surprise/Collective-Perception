source("functions.R")

dir_names <-
  c("penalty_020", 
   "penalty_034", 
   "no_penalty_034",
   "penalty_020_superlong",
   "penalty_034_superlong")

penalty_vec <- c(T, T, F, T, T)

dfs <- create_fitness_df(dir_names, penalty_vec)

best_fitness_df <- dfs['best_fitness']
write.csv(best_fitness_df, "final_data/best_fitness_df.csv")
saveRDS(best_fitness_df, "final_data/best_fitness.RDS")

all_fitness_df <- dfs['all_fitness']
write.csv(all_fitness_df, "final_data/all_fitness_df.csv")
saveRDS(all_fitness_df, "final_data/all_fitness.RDS")

eval_round_df <- dfs['eval']['eval']
write.csv(eval_round_df, "final_data/eval_round_df.csv")
saveRDS(eval_round_df, "final_data/eval_round.RDS")

create_fitness_df <- function(dir_names, penalty_vec) {
  best_fitness_df <- data.frame()
  all_fitness_df <- data.frame()
  eval_round_df <- data.frame()
  
  for(i in 1:length(dir_names)) {
    dir_name <- dir_names[i]
    penalize_fitness_mode <- penalty_vec[i]
    
    print(paste(">> Starting with ", dir_name, "| penalized?", penalize_fitness_mode))
    
    dirs <- c(
      paste0(
        "final_raw_data/",dir_name, "/run", 
        0:9, "/")
    )
    
    json_import_error_files <- c()
    
    for (path_to_dir in dirs) {
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
        mutate(generationCounter = as.integer(generationCounter),
               evoRunId = path_to_dir,
               settingId = dir_name)
      
      all_fitness_data <- json_df %>%
        select(generationCounter, completeFitnessVals, completeFitnessIDs) %>%
        unnest(c(completeFitnessVals, completeFitnessIDs)) %>% 
        mutate(id = str_split(completeFitnessIDs, "\\.") %>% map_chr(., 1),
               evoRunId = path_to_dir,
               settingId = dir_name)
      
      eval_round_data <- json_df %>% 
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
        mutate(penalizedFitness = calc_penalized_fitness(normFitness, correctOpinionPercentage)) %>%
        mutate(evoRunId = path_to_dir,
               settingId = dir_name,
               penalized = penalize_fitness_mode)
       
      best_fitness_df <- rbind(best_fitness_df, best_fitness_data)
      all_fitness_df <- rbind(all_fitness_df, all_fitness_data)
      eval_round_df <- rbind(eval_round_df, eval_round_data)
      print(paste("     #", path_to_dir, "done"))
    }
  }
  
  return(list("best_fitness" = best_fitness_df, 
              "all_fitness" = all_fitness_df,
              "eval" =  eval_round_df))
}
