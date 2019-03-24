using Progopoly.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Progopoly.Models
{
    public class Deck
    {
        protected IGameLog _gameLog;
        protected Queue<Action<GameState>> _cardsQueue { get; set; }
        protected class Card : Attribute { }

        public Deck(IGameLog gameLog)
        {
            _gameLog = gameLog;
            InitializeDeck();
        }

        protected void InitializeDeck()
        {
            var cardMethods = GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => Attribute.GetCustomAttribute(m, typeof(Card)) != null)
                .Select(m => (Action<GameState>)Delegate.CreateDelegate(typeof(Action<GameState>), this, m))
                .ToList();

            cardMethods.Shuffle();

            _cardsQueue = new Queue<Action<GameState>>(cardMethods);
        }

        public void DrawCard(GameState gameState)
        {
            if (!_cardsQueue.Any())
                InitializeDeck();

            var cardAction = _cardsQueue.Dequeue();
            cardAction(gameState);
        }
    }
}
