import itertools
import sys

if __name__ == "__main__":
	
	args = sys.argv[1:]

	if (len(args)==0):
		vm_name ="defaultVM"
	else:
		vm_name = args[0]


	# TBD settings
	task_difficulty = 0.2 # 0.34 and 0.2
	seed_start = 100
	n_evo_runs = 10
	penalize_fitness = "True"

	# Fixed settings
	m_rate = 0.2
	n_gen = 300
	pop_size = 50
	fitness_calc = "MinFitness"
	sim_time = 200

	win_cmds = []
	linux_cmds = []

	for i in range(n_evo_runs):
		seed = seed_start + i

		run_id = f'{vm_name}-no{i}--task_diff={task_difficulty}-penalty-{penalize_fitness}'

		lines = [
			f"runID : {run_id}",
			f"evoMutationProb : {m_rate}",
			f"evoPopulationSize : {pop_size}",
			f"evoNGenerations : {n_gen}",
			f"fitnessCalc: {fitness_calc}",
			f"simTime : {sim_time}",
			f"randomSeed: {seed}",
			f"penalizeFitness: {penalize_fitness}",
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
