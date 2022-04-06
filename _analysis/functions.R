### Libraries
library(ggraph)
library(igraph)
library(tidyverse)
library(tidyjson)
library(ggplot2)
library(data.table)


### Functions & Fixed Vars

#  *_Parameters.txt
params_obj_from_filepath <- function(filepath) {
  params_lines <- read_lines(filepath)
  
  params_obj <- list()
  for (line in params_lines) {
    split <- str_split(line, " : ")
    key <- split[[1]][1]
    val <- split[[1]][2]
    print(paste("key:", key, "| val:", val))
    params_obj[key] <- val
  }
  return(params_obj)
}

# *_PositionsOpinions.csv
prep_opinion_position_data <- function(file, decision_rule) {
  df <- read_csv2(file) %>%
    mutate(run = as.factor(run))
  
  df <- df %>% 
    mutate(opinion_int = recode(df$opinion, Black = 1, White = 0),
           runID = paste0(run,"_", arenaIndex),
           decision_rule = decision_rule)
  
  return(df)
}

fast_prep_opinion_position_data <- function(file, decision_rule) {
  # run;time;botname;arenaIndex;botIndex;x;y;opinion
  dt <- fread(file, colClasses = c(NA, NA, NA, NA, NA, "NULL", "NULL", NA))
  
  dt[, run := as.factor(run)]
  dt[, opinion_int := recode(opinion, Black = 1, White = 0)]
  dt[, runID := paste0(run,"_", arenaIndex)]
  dt[, decision_rule := decision_rule]
  
  return(dt)
}

# *_neural_net_log.csv
nnlog_id_cols <- c("time", "run", "arenaIndex","botIndex", "networkID" , "currentOpinion", "newOpinion", 
                   "tilesInversed")

dec_inputs <- c("normalized_msg_number", "observed_opinion_dist",  "ground_sensor", "last_decision")
dec_outputs <- c("new_opinion")
pred_inputs <- c("normalized_msg_number", "observed_opinion_dist",  "ground_sensor", "current_decision")
pred_outputs <- 
  c("normalized_msg_number", "observed_opinion_dist", "ground_sensor")
  #c("ground_sensor")

preprocess_neuralnetlog_df <- function(df, pred_or_dec) {
  if (pred_or_dec == "pred") {
    df <- df %>%
      select(nnlog_id_cols, predNetInput, predNetOutput, tilesInversed) %>% 
      mutate(predNetInput = str_remove(predNetInput, "\\[") %>% str_remove("\\]"),
             predNetOutput = str_remove(predNetOutput, "\\[") %>% str_remove("\\]")) %>%
      separate(predNetInput, paste0("i_", pred_inputs), sep=",", convert=T) %>%
      separate(predNetOutput, paste0("o_", pred_outputs), sep=",", convert=T)
  } else if (pred_or_dec == "dec") {
    df <- df %>%
      select(nnlog_id_cols, decNetInput, decNetOutput, tilesInversed) %>% 
      mutate(decNetInput = str_remove(decNetInput, "\\[") %>% str_remove("\\]"),
             decNetOutput = str_remove(decNetOutput, "\\[") %>% str_remove("\\]")) %>%
      separate(decNetInput, paste0("i_", dec_inputs), sep=",", convert=T) %>%
      separate(decNetOutput, paste0("o_", dec_outputs), sep=",", convert=T)
  }
  
  return(df)
}

# _evolution_history.json
gen_edge_df <- function(edges) {
  ret <- list()
  
  for (parent in edges[[1]]) {
    
    ret[[length(ret) + 1]] <- c("origin", parent)
  }
  
  for (i in 1:(length(edges)-1)) { # through generations
    for(from in edges[[i]]) { # from generation i
      for(to in edges[[i+1]]) { # to generation i+1
        # print(paste("from:", from, "|", "to:",to))
        if (i == 1) {
          if (str_starts(to, paste0(from,"\\."))) {
            ret[[length(ret) + 1]] <- c(from, to)  
          }
        } else {
          if (str_starts(to, from)) {
            ret[[length(ret) + 1]] <- c(from, to)
          }  
        }
      }
    }
  }
  
  return(ret)
}

calc_penalized_fitness <- function(fitness, percentage) {
  max_penalty <- 2
  factor <- (1/max_penalty) + percentage * ( 1 - 1 / max_penalty)
  
  penalized_fitness <- fitness * factor
  return(penalized_fitness)
}
