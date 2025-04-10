using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTree
{
    /// <summary>
    /// Database of all power nodes in the game
    /// </summary>
    [CreateAssetMenu(fileName = "PowerNodeDatabase", menuName = "PowerTree/Node Database")]
    public class PowerNodeDatabase : ScriptableObject
    {
        public PowerNode[] AllNodes;
        
        // Optional: cached nodes by branch
        private Dictionary<PowerBranch, List<PowerNode>> nodesByBranch;
        
        public void Initialize()
        {
            nodesByBranch = new Dictionary<PowerBranch, List<PowerNode>>();
            
            foreach (PowerBranch branch in Enum.GetValues(typeof(PowerBranch)))
            {
                nodesByBranch[branch] = new List<PowerNode>();
            }
            
            foreach (var node in AllNodes)
            {
                nodesByBranch[node.branch].Add(node);
            }
        }
        
        public List<PowerNode> GetNodesByBranch(PowerBranch branch)
        {
            if (nodesByBranch == null)
            {
                Initialize();
            }
            
            return nodesByBranch[branch];
        }
    }
}