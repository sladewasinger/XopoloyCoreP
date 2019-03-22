using Progopoly.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Progopoly.Models
{
    public class GameState
    {
        private int _currentPlayerListIndex;
        public List<Player> Players { get; set; }
        public List<Tile> Tiles { get; set; }
        public int CurrentPlayerListIndex
        {
            get
            {
                return _currentPlayerListIndex;
            }
            set
            {
                _currentPlayerListIndex = value;
                if (_currentPlayerListIndex >= Players.Count())
                {
                    _currentPlayerListIndex = 0;
                }
            }
        }
        public Player CurrentPlayer
        {
            get
            {
                if (Players.Any())
                    return Players[CurrentPlayerListIndex];
                else
                    return null;
            }
        }
        public int CurrentFrameId { get; set; }
        public bool WaitForBuyOrAuctionStart { get; set; }
        public Tile CurrentTile
        {
            get
            {
                if (CurrentPlayer == null)
                    return null;
                return Tiles[CurrentPlayer.BoardPosition];
            }
        }
        public bool AuctionInProgress
        {
            get
            {
                return Auction != null;
            }
        }
        public Auction Auction { get; set; }
        public List<TradeOffer> TradeOffers { get; set; }
        public CommunityChestDeck CommunityChestDeck { get; set; }
        public ChanceDeck ChanceDeck { get; set; }
        public bool GameFinished { get; set; }
    }
}