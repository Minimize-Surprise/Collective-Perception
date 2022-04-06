import itertools

if __name__ == "__main__":
	m_rates = [0.2, 0.3]
	n_gens = [300]
	pop_sizes = [50]
	fitness_calcs = ["MinFitness"]
	task_difficulty = 0.34
	sim_time = 200

	product = itertools.product(m_rates, n_gens, pop_sizes, fitness_calcs)

	win_cmds = []
	linux_cmds = []
	
	for i, element in enumerate(product):
		m_rate = element[0]
		n_gen = element[1]
		pop_size = element[2]
		fitness_calc = element[3]

		run_id = f"no{i}--m_rate={m_rate}|n_gen={n_gen}|pop_size={pop_size}|fitness_calc={fitness_calc}"

		lines = [
			f"runID : {run_id}",
			f"evoMutationProb : {m_rate}",
			f"evoPopulationSize : {pop_size}",
			f"evoNGenerations : {n_gen}",
			f"fitnessCalc: {fitness_calc}",
			f"simTime : {sim_time}",
			f"taskDifficulty : {task_difficulty}"
		]

		filename = f"{i}.yaml"
		with open(filename, 'w') as f:
			for item in lines:
				f.write("%s\n" % item)

		win_cmds.append(f"call Builder.exe -batchmode -nographics -params {i}.yaml")
		linux_cmds.append(f"./ms_linux.x86_64 -batchmode -nographics -params {i}.yaml")

		print(f"#{i} - runID={run_id} done")

	with open("run.sh", 'w') as f:
		f.write("# comment out as needed\n")
		f.write("\n".join(linux_cmds))
		
	with open("run.bat", 'w') as f:
		f.write(":: comment out as needed\n")
		f.write("\n".join(win_cmds))