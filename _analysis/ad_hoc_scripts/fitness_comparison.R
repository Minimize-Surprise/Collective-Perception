source("functions.R")

files <- c(
  "data/2021_08_24_fitness_comparison/5.json",
  "data/2021_08_24_fitness_comparison/6.json",
  "data/2021_08_24_fitness_comparison/12.json",
  "data/2021_08_24_fitness_comparison/13.json"
)

runs <- c(
  "runID=no05--m_rate=0.1|pop_size=10",
  "runID=no06--m_rate=0.1|pop_size=25",
  "runID=no12--m_rate=0.2|pop_size=50",
  "runID=no13--m_rate=0.2|pop_size=100"
)


final_df <- data.frame()

for (i in 1:length(files)) {
  evo_hist_path <- files[i]
  run_name <- runs[i]
  
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
           run_name = run_name)
  
  all_fitness_data <- json_df %>%
    select(generationCounter, completeFitnessVals, completeFitnessIDs) %>%
    unnest(c(completeFitnessVals, completeFitnessIDs)) %>% 
    mutate(id = str_split(completeFitnessIDs, "\\.") %>% map_chr(., 1), 
           run_name = run_name)
  
  
  final_df <- rbind(final_df, best_fitness_data)
  
  print(paste(evo_hist_path, "done!"))
}

# Best Fitness Plot
ggplot(final_df, aes(x=generationCounter, y=bestFitness, color=run_name)) +
  geom_line() +
  theme_bw() +
  labs(x = "generation", y = "generation's best fitness",
       title="Best fitness over generations") +
  scale_y_continuous(breaks=seq(0,1,0.1), limits = c(0,1))
