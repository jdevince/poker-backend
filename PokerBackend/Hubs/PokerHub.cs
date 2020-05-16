using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PokerBackend.Models;
using PokerBackend.Services;
using System.Text.RegularExpressions;

namespace PokerBackend.Hubs
{
    public class PokerHub : Hub
    {
        private readonly IHubContext<PokerHub> _hubContext;
        private PokerService _pokerService;

        public PokerHub(PokerService pokerService, IHubContext<PokerHub> hubContext)
        {
            _pokerService = pokerService;
            _hubContext = hubContext;
        }

        public string JoinGame(string username, string gameCode)
        {
            gameCode = gameCode.ToUpper();
            Game game = _pokerService.Games.FirstOrDefault(x => x.GameCode == gameCode);

            if (game == null)
            {
                return "That game code does not exist";
            }
            else if (game.Players.Any(x => x.Username.ToUpper() == username.ToUpper()))
            {
                return "That username is already taken for this game";
            }
            else
            {
                Player player = new Player(username, Context.ConnectionId);
                game.Players.Add(player);
                Groups.AddToGroupAsync(Context.ConnectionId, gameCode);
                return "Success";
            }
        }

        public void SitDown(int seatNumber, double buyInAmount)
        {
            Player player = _pokerService.GetCurrentPlayer(Context.ConnectionId);
            player.Seat = seatNumber;
            player.Chips = buyInAmount;
            SendGameStateUpdate();
        }

        public void StartGame()
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            new Task(() => { PlayGame(game); }).Start();
        }

        public void GetCurrentGameState()
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            _hubContext.Clients.Client(Context.ConnectionId).SendAsync("gameStateChange", game);
        }

        private override Task OnDisconnectedAsync(Exception exception)
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);

            if (game != null)
            {
                Player player = _pokerService.GetCurrentPlayer(Context.ConnectionId);
                game.Players.Remove(player);

                if (game.Players.Count == 0)
                {
                    _pokerService.DeleteGame(game);
                }
                else
                {
                    SendGameStateUpdate();
                }
            }

            _pokerService.CheckForStaleGames(); //Unrelated to this game, but safety check to avoid memory leak

            return base.OnDisconnectedAsync(exception);
        }

        private void PlayGame(Game game)
        {
            while (game.Players.Count > 0)
            {
                PlayHand(game);
            }
        }

        private void PlayHand(Game game)
        {
            Clients.Group(game.GameCode).SendAsync("newHand");
            var deck = _pokerService.GetFullDeck();

            Deal(game, deck);
        }

        private void Deal(Game game, List<Card> deck)
        {
            var players = game.Players.Where(x => x.Seat != null).OrderBy(x => x.Seat);

            for (int i = 0; i < 2; i++)
            {
                foreach (Player player in players)
                {
                    player.Hand[i] = _pokerService.GetCardFromDeck(deck);
                }
            }
        }

        private void SendGameStateUpdate()
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            Clients.Group(game.GameCode).SendAsync("gameStateUpdate", game);
        }
    }
}
