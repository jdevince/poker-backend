using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerBackend.Models
{
    public class Player
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public int? Seat { get; set; }
        public double Chips { get; set; }
        public Card[] Hand { get; set; }

        public Player(string username, string connectionId)
        {
            this.Username = username;
            this.ConnectionId = connectionId;
            this.Hand = new Card[2];
        }
    }
}
