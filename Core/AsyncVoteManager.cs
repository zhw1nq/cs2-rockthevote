﻿namespace cs2_rockthevote
{
    public enum VoteResultEnum
    {
        Added,
        AlreadyAddedBefore,
        VotesAlreadyReached,
        VotesReached,
        InvalidMap
    }

    public record VoteResult(VoteResultEnum Result, int VoteCount, int RequiredVotes);

    public class AsyncVoteValidator
    {
        private readonly float _votePercentage;

        public AsyncVoteValidator(float votePercentage)
        {
            _votePercentage = votePercentage;
        }

        public int RequiredVotes(int totalPlayers)
        {
            return (int)Math.Ceiling(totalPlayers * _votePercentage);
        }

        public bool CheckVotes(int numberOfVotes, int totalPlayers)
        {
            return numberOfVotes > 0 && numberOfVotes >= RequiredVotes(totalPlayers);
        }
    }

    public class AsyncVoteManager
    {
        private readonly List<int> votes = new();
        private readonly AsyncVoteValidator _voteValidator;

        public int VoteCount => votes.Count;
        public int RequiredVotes => _voteValidator.RequiredVotes(ServerManager.ValidPlayerCount());
        public bool VotesAlreadyReached { get; private set; } = false;

        public AsyncVoteManager(int votePercentage)
        {
            _voteValidator = new AsyncVoteValidator(votePercentage / 100f);
        }

        public void OnMapStart(string _mapName)
        {
            votes.Clear();
            VotesAlreadyReached = false;
        }

        public VoteResult AddVote(int userId)
        {
            if (VotesAlreadyReached)
                return new VoteResult(VoteResultEnum.VotesAlreadyReached, VoteCount, RequiredVotes);

            VoteResultEnum result;
            if (votes.Contains(userId))
                result = VoteResultEnum.AlreadyAddedBefore;
            else
            {
                votes.Add(userId);
                result = VoteResultEnum.Added;
            }

            int totalPlayers = ServerManager.ValidPlayerCount();
            if (_voteValidator.CheckVotes(votes.Count, totalPlayers))
            {
                VotesAlreadyReached = true;
                return new VoteResult(VoteResultEnum.VotesReached, VoteCount, RequiredVotes);
            }

            return new VoteResult(result, VoteCount, RequiredVotes);
        }

        public void RemoveVote(int userId)
        {
            votes.Remove(userId);
        }
    }
}