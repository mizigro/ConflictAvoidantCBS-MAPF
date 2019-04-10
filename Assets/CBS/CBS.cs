﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CBS : MonoBehaviour{

	public Transform[] agents;
    public Transform[] targets;
	float waitTime = 1.0f;

	Vector3[] agentPositions;

	private bool cbsComplete=true;
	int nAgents;

	Grid grid;

	void Awake() {
		nAgents = agents.Length<targets.Length?agents.Length:targets.Length;

		agentPositions = new Vector3[agents.Length];
		for(int i=0;i<agentPositions.Length;i++)
			agentPositions[i] = new Vector3(agents[i].position.x, agents[i].position.y, agents[i].position.z);


		// get the whole grid layout
		grid = GetComponent<Grid> ();
	}

	void Update() {
		// main algorithm
		if (Input.GetKeyDown (KeyCode.Return)) {
			if(cbsComplete){
				cbsComplete = false;
				// reset agent positions
				for(int i=0;i<agents.Length;i++)
					agents[i].position = new Vector3(agentPositions[i].x, agentPositions[i].y, agentPositions[i].z);
				StartCoroutine(StepCBS());
			}
		}
	}

	List<List<Node>> GetSolution(List<State>[] constraints){
		List<List<Node>> solution = new List<List<Node>> ();
		for(int i = 0; i <constraints.Length; i++){
			grid.ResetNodes();
			// find min path with the given constraints
			List<Node> path = AStar.FindMinPath (agents[i].position, targets[i].position, grid, constraints[i]);
			solution.Add(path);
		}
		return solution;
	}

	int GetSolutionCost(List<List<Node>> paths){
		int cost=0;
		foreach(List<Node> path in paths)
			cost += path.Count;
		return cost;
	}

	/* get min cost node
	 * from list of nodes */
	static CTNode GetMinCostNode(List<CTNode> nodes){
		CTNode node = nodes[0];
		for (int i = 1; i < nodes.Count; i ++) 
			if (nodes[i].cost <= node.cost) 
					node = nodes[i];

		return node;
	}

	IEnumerator StepCBS(){
		grid.paths = null;

		List<CTNode> OPEN = new List<CTNode> ();
		

		/* empty constraints */
		List<State>[] emptyConstraints = new List<State> [nAgents];
		for(int i=0;i<emptyConstraints.Length;i++){
			emptyConstraints[i] = new List<State>();
		}

		List<List<Node>> solution = GetSolution(emptyConstraints);
		int cost = GetSolutionCost(solution);
		
		CTNode root = new CTNode(emptyConstraints, solution, cost);

		OPEN.Add(root);

		while(OPEN.Count > 0){

			CTNode curNode = GetMinCostNode(OPEN);
			OPEN.Remove(curNode);

			grid.paths = curNode.solution;
			yield return new WaitForSeconds(waitTime); // show for a few seconds

			List<Conflict> curConflicts = GetConflicts(curNode.solution);
			Debug.Log(curNode.cost);
			if(curConflicts.Count == 0){
				// solution found
				Debug.Log("Solution Found.");
				break;
			}
			else if(curConflicts.Count == 1){
				Conflict conflict = curConflicts[0];
				foreach(int agentID in conflict.agents){
					// copy constraints
					List<State>[] newConstraints = new List<State>[nAgents];
					for(int i=0;i<newConstraints.Length;i++){
						newConstraints[i] = new List<State>(curNode.constraints[i]);
					}
					// add new constraint
					newConstraints[agentID].Add(new State(conflict.node, conflict.time));

					List<List<Node>> newSolution = GetSolution(newConstraints);
					int newCost = GetSolutionCost(newSolution);
					CTNode newNode = new CTNode(newConstraints, newSolution, newCost);
					OPEN.Add(newNode);
				}
			}
		}
		
		StartCoroutine(FlyDrones());
	}

	IEnumerator FlyDrones(){
		for(int t=0,count=0;;t++,count=0){
			for(int i=0;i<grid.paths.Count;i++){
				// time steps left
				if(t < grid.paths[i].Count){
					count++;
					agents[i].position = grid.paths[i][t].worldPosition;
				}
			}
			yield return new WaitForSeconds(waitTime);
			if(count==0)break;
		}
		
		cbsComplete = true;
	}

	List<Conflict> GetConflicts(List<List<Node>> paths){
		List<Node> curNodes = new List<Node>();
		List<Conflict> conflicts = new List<Conflict>();

		bool conflictFound = false;

		int t = 0; // time step
		do{
			curNodes.Clear();

			// add all nodes of current 
			// time step left in each path
			foreach(List<Node> path in paths)
				if(t < path.Count)
					curNodes.Add(path[t]);

			// check for conflicts
			for(int i=0;i<curNodes.Count && !conflictFound;i++)
				for(int j=0;j<curNodes.Count && !conflictFound;j++)
					if(i!=j && curNodes[i]==curNodes[j]){
						List<int> curAgents = new List<int> ();
						curAgents.Add(i);
						curAgents.Add(j);
						conflicts.Add(new Conflict(curAgents, curNodes[i], t+1));
						conflictFound = true;
					}
				
			t += 1;
		}while(curNodes.Count>1 && !conflictFound);

		return conflicts;
	}
}