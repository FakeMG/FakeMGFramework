using System;
using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    public sealed class AsyncSaveParticipantDependencyOrderResolver
    {
        public IReadOnlyList<IAsyncSaveParticipant> Resolve(IEnumerable<IAsyncSaveParticipant> participants)
        {
            Dictionary<string, IAsyncSaveParticipant> participantsById = new(StringComparer.Ordinal);
            foreach (IAsyncSaveParticipant participant in participants)
            {
                if (participant == null || string.IsNullOrWhiteSpace(participant.ParticipantId))
                {
                    throw new InvalidOperationException("Every save participant requires a stable participant ID.");
                }

                if (!participantsById.TryAdd(participant.ParticipantId, participant))
                {
                    throw new InvalidOperationException($"Duplicate save participant ID '{participant.ParticipantId}'.");
                }
            }

            List<IAsyncSaveParticipant> orderedParticipants = new(participantsById.Count);
            HashSet<string> visitingIds = new(StringComparer.Ordinal);
            HashSet<string> visitedIds = new(StringComparer.Ordinal);
            foreach (IAsyncSaveParticipant participant in participantsById.Values)
            {
                VisitParticipant(
                    participant,
                    participantsById,
                    visitingIds,
                    visitedIds,
                    orderedParticipants);
            }

            return orderedParticipants;
        }

        private static void VisitParticipant(
            IAsyncSaveParticipant participant,
            IReadOnlyDictionary<string, IAsyncSaveParticipant> participantsById,
            ISet<string> visitingIds,
            ISet<string> visitedIds,
            ICollection<IAsyncSaveParticipant> orderedParticipants)
        {
            if (visitedIds.Contains(participant.ParticipantId))
            {
                return;
            }

            if (!visitingIds.Add(participant.ParticipantId))
            {
                throw new InvalidOperationException($"Save participant dependency cycle contains '{participant.ParticipantId}'.");
            }

            foreach (string dependencyId in participant.RunsAfterParticipantIds ?? Array.Empty<string>())
            {
                if (!participantsById.TryGetValue(dependencyId, out IAsyncSaveParticipant dependency))
                {
                    throw new InvalidOperationException($"Save participant '{participant.ParticipantId}' depends on missing participant '{dependencyId}'.");
                }

                VisitParticipant(dependency, participantsById, visitingIds, visitedIds, orderedParticipants);
            }

            visitingIds.Remove(participant.ParticipantId);
            visitedIds.Add(participant.ParticipantId);
            orderedParticipants.Add(participant);
        }
    }
}
