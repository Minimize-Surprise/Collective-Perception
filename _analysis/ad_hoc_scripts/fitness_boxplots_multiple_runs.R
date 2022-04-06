source("functions.R")

dir_name <-
  #"2021_10_11_penalty_020"
  "2021_10_11_penalty_034"
  #"2021_10_11_no_penalty_034"

penalize_fitness_mode <- T # boolean: T or F

plot_file <- paste0(dir_name,"_best_fitness_new_diff")

dirs <- c(
  paste0(
    "../../data/",dir_name, "/run", 
    0:9, "/")
)

json_import_error_files <- c()

df <- data.frame()

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
           evoRunId = path_to_dir)
  
  all_fitness_data <- json_df %>%
    select(generationCounter, completeFitnessVals, completeFitnessIDs) %>%
    unnest(c(completeFitnessVals, completeFitnessIDs)) %>% 
    mutate(id = str_split(completeFitnessIDs, "\\.") %>% map_chr(., 1))
  
  df <- rbind(df, best_fitness_data)
  print(paste(">>", path_to_dir, "done"))
}

n_evo_runs <- length(unique(df$evoRunId))
ggplot(df, 
       aes(x=generationCounter, y=bestFitness, group=generationCounter)) +
  geom_boxplot() +
  theme_bw() +
  labs(x = "generation", y = "generation's best fitness",
       title="Best fitness over generations",
       subtitle=paste("Over", n_evo_runs, "evolutionary runs - penalty in analysis? ", penalize_fitness_mode, " - setting: ", dir_name)
       ) +
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1))

ggsave(paste0(plot_file, "_boxplot.png"),
       width=18, height=10, unit="in")

ggplot(df, 
       aes(x=generationCounter, y=bestFitness, color=evoRunId)) +
  geom_line() +
  theme_bw() +
  labs(x = "generation", y = "generation's best fitness",
       title="Best fitness over generations",
       subtitle=paste("Over", n_evo_runs, "evolutionary runs - penalty in analysis? ", penalize_fitness_mode, " - setting: ", dir_name)
  ) +
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1)) +
  theme(legend.position = "none")

ggsave(paste0(plot_file, "_lineplot.png"),
       width=18, height=10, unit="in")
