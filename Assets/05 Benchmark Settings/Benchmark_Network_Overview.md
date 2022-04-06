# Benchmark Candidate Selection

## How do we select the ANN pairs used in the benchmark?

From each run take the median (round up in case of even number of runs) best performing network.

> **Example**: 10 runs means we choose the run with rank 6 as median candidate.

## Results

```
  settingId             runId completeFitnessIDs                                                                    
1 no_penalty_034        run2  #30.0.0.0.0.2.0.1.1.2.0.0.0.0.0.0.2.0.0.1.0.0.2.0.1.0.0.0.0.0.0.0.3.0.0.1.2.0.0.0.1.
2 penalty_020           run3  #32.0.0.2.0.0.0.0.0.1.0.1.0.0.0.0.0.0.0.3.0.0.0.2.0.2.0.0.1.0.0.0.1.1.1.0.0.1.0.0.1.~
3 penalty_020_superlong run3  #47.0.0.0.1.0.0.1.0.2.0.1.0.0.0.0.0.0.3.0.0.1.0.0.0.2.2.0.0.2.0.0.0.0.3.0.0.2.0.0.4.
4 penalty_034           run5  #25.0.1.0.1.0.2.2.0.2.0.0.1.0.0.1.0.0.0.3.0.0.4.0.0.1.1.0.0.0.1.1.1.0.1.0.0.1.0.0.0.
5 penalty_034_superlong run5  #42.0.0.0.1.0.0.0.0.0.0.2.1.0.0.0.0.0.0.1.0.0.1.1.0.1.0.0.0.1.0.0.0.0.0.0.0.0.1.0.2.
```

