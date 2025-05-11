using UnityEngine.AI;

namespace FakeMG.ExtensionMethods {
    public static class NavMeshAgentExtensions {
        public static bool IsNavigationComplete(this NavMeshAgent agent) {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        }
    }
}