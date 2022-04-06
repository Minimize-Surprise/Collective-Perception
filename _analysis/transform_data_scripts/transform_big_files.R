source("functions.R")

big_files <- c(
    "../../data/big_files/no_penalty_034/run2/21-10-07_09-50-16_neural_net_log.csv",
    "../../data/big_files/no_penalty_034/run2/21-10-07_09-50-16_PositionsOpinions.csv",
    "../../data/big_files/penalty_020/run7/21-10-07_09-44-23_neural_net_log.csv",
    "../../data/big_files/penalty_020/run7/21-10-07_09-44-23_PositionsOpinions.csv",
    "../../data/big_files/penalty_020_superlong/run4/21-10-13_12-23-40_neural_net_log.csv",
    "../../data/big_files/penalty_020_superlong/run4/21-10-13_12-23-40_PositionsOpinions.csv"
  )

out_names <- c(
  "no_penalty_034_run2_neural_net_reduced",
  "no_penalty_034_run2_opinions_reduced",
  "penalty_020_run7_neural_net_reduced",
  "penalty_020_run7_opinions_reduced",
  "penalty_020_superlong_run4_neural_net_reduced",
  "penalty_020_superlong_run4_opinions_reduced"
)

arena_settings_files <- c(
  "../../data/big_files/no_penalty_034/run2/21-10-07_09-50-16_arena_settings.csv",
  "../../data/big_files/no_penalty_034/run2/21-10-07_09-50-16_arena_settings.csv",
  "../../data/big_files/penalty_020/run7/21-10-07_09-44-23_arena_settings.csv",
  "../../data/big_files/penalty_020/run7/21-10-07_09-44-23_arena_settings.csv",
  "../../data/big_files/penalty_020_superlong/run4/21-10-13_12-23-40_arena_settings.csv",
  "../../data/big_files/penalty_020_superlong/run4/21-10-13_12-23-40_arena_settings.csv"
)

max_gens <- c(
  300, 300,
  300, 300,
  600, 600
)

chunked_import <- T

for (i in 1:length(big_files)) {
  big_file_path <- big_files[i]
  print(paste0("!!!! Starting with new file", big_file_path))
  
  arena_settings_path <- arena_settings_files[i]
  arena_settings_df <- read_csv2(arena_settings_path)
  
  out_name <- out_names[i]
  
  max_gen <- max_gens[i]
  n_gen_strips <- 5
  
  out_df <- data.frame()
  cols <- names(read.csv2(big_file_path, head=T, nrows=1)[-1, ])
  
  if(chunked_import) {
    con <- file(big_file_path, "r")  
    count <- 0
    while(T) {
      count <- count + 500000
      print(paste("Importing till row", count))
      import <- read.csv2(con, nrows = 500000, header=T, col.names=cols)
      if (nrow(import) == 0) {
        break
      }
      curr_df <- import %>%
        left_join(arena_settings_df, by=c("arenaIndex", "run" = "round")) %>%
        filter(generationIndex %in% c(0:n_gen_strips, (max_gen-n_gen_strips): max_gen))
      
      out_df <- rbind(out_df, curr_df)
    }
    
    close(con)
  } else {
    import <- read.csv2(big_file_path)
    curr_df <- import %>%
      left_join(arena_settings_df, by=c("arenaIndex", "run" = "round")) %>%
      filter(generationIndex %in% c(0:n_gen_strips, (max_gen-n_gen_strips): max_gen))
    
    out_df <- rbind(out_df, curr_df)
  }
  
  write.csv(out_df, paste0("final_data/", out_name,".csv"))
  saveRDS(out_df, paste0("final_data/", out_name,".RDS"))
}