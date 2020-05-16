using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using PokerBackend.Models;
using PokerBackend.Enums;

namespace PokerBackend.Services
{
    public class PokerService
    {
        private static readonly Random _random = new Random();
        private static readonly HttpClient _httpClient = new HttpClient();

        public List<Game> Games = new List<Game>();

        public PokerService()
        {

        }

        public string CreateNewGame()
        {
            string newGameCode = GetRandomGameCode();

            if (Games.Select(x => x.GameCode).Contains(newGameCode))
            {
                newGameCode = CreateNewGame();
            }
            else
            {
                Games.Add(new Game(newGameCode));
            }

            return newGameCode;
        }

        private string GetRandomGameCode()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int length = 3;

            string newGameCode = new string(Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());

            return newGameCode;
        }

        public Game GetCurrentGame(string connectionId)
        {
            foreach(Game game in Games)
            {
                foreach (Player player in game.Players)
                {
                    if (player.ConnectionId == connectionId)
                    {
                        return game;
                    }
                }
            }

            return null;
        }

        public void DeleteGame(Game game)
        {
            Games.Remove(game);
        }

        public void CheckForStaleGames()
        {
            int gameCount = Games.Count;
            for (int i = 0; i < gameCount; i++)
            {
                if (i < Games.Count)
                {
                    Game game = Games[i];
                    if (game.Players == null || game.Players.Count == 0)
                    {
                        DeleteGame(game);
                        i--;
                        gameCount--;
                    }
                }
            }
        }

        public Player GetCurrentPlayer(string connectionId)
        {
            foreach (Game game in Games)
            {
                foreach (Player player in game.Players)
                {
                    if (player.ConnectionId == connectionId)
                    {
                        return player;
                    }
                }
            }

            return null;
        }

        public Player GetPlayer(Game game, string username)
        {
            foreach (Player player in game.Players)
            {
                if (player.Username == username)
                {
                    return player;
                }
            }

            return null;
        }

        public List<Card> GetFullDeck()
        {
            var deck = new List<Card>();

            var suits = Enum.GetValues(typeof(Suit));
            var cardValues = Enum.GetValues(typeof(CardValue));

            foreach (Suit suit in suits)
            {
                foreach(CardValue cardValue in cardValues)
                {
                    deck.Add(new Card(cardValue, suit));
                }
            }

            return deck;
        }

        public Card GetCardFromDeck(List<Card> deck)
        {
            int index = _random.Next(deck.Count);
            Card card = deck[index];
            deck.RemoveAt(index);
            return card;
        }
    }
}
