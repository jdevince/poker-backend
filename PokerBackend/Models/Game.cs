using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerBackend.Models
{
    public class Game
    {
        public static readonly int MAX_SEATS = 9;
        
        public string GameCode { get; set; }
        public double MinBuyIn { get; set; }
        public double MaxBuyIn { get; set; }
        public double BigBlind { get; set; }
        public double SmallBlind { get; set; }
        public bool IsStarted { get; set; }
        public List<Player> Players { get; set; }

        public int DealerSeat { get; set; }
        public double Pot { get; set; }
        public Card[] Board { get; set; }
        

        public Game(string gameCode)
        {
            this.GameCode = gameCode;
            this.Players = new List<Player>();
            Board = new Card[5];
        }
    }
}
