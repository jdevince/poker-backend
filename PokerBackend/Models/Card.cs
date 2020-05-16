using PokerBackend.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerBackend.Models
{
    public class Card
    {
        public CardValue CardValue { get; set; }
        public Suit Suit { get; set; }

        public Card(CardValue cardValue, Suit suit)
        {
            CardValue = cardValue;
            Suit = suit;
        }
    }
}
