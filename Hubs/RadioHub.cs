using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TatehamaRadioServer.Hubs
{
    // ルーム情報を保持するためのクラス
    public class ConnectionRoom
    {
        public string? PcConnectionId { get; set; }
        public string? PhoneConnectionId { get; set; }
    }

    public class RadioHub : Hub
    {
        // ★ スレッドセーフなDictionaryでルーム情報を管理
        private static readonly ConcurrentDictionary<string, ConnectionRoom> _rooms = new ConcurrentDictionary<string, ConnectionRoom>();

        // PCがルーム作成をリクエスト
        public async Task CreateRoom()
        {
            string roomId;
            do
            {
                // ランダムな6桁のルームIDを生成
                roomId = new Random().Next(100000, 999999).ToString();
            } while (!_rooms.TryAdd(roomId, new ConnectionRoom { PcConnectionId = Context.ConnectionId }));

            // ルーム（グループ）にPCを追加
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // リクエスト元のPCにだけ、作成されたルームIDを通知
            await Clients.Caller.SendAsync("RoomCreated", roomId);
            Console.WriteLine($"[Hub] Room {roomId} created by PC {Context.ConnectionId}");
        }

        // スマホがルーム参加をリクエスト
        public async Task JoinRoom(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room) && room.PhoneConnectionId == null)
            {
                // ルームにスマホの情報を追加
                room.PhoneConnectionId = Context.ConnectionId;
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // 同じルームのPCとスマホの両方に接続確立を通知
                await Clients.Group(roomId).SendAsync("ConnectionEstablished");
                Console.WriteLine($"[Hub] Phone {Context.ConnectionId} joined room {roomId}");
            }
            else
            {
                // ルームが存在しない、または満員の場合
                await Clients.Caller.SendAsync("Error", "ルームが見つからないか、既に使用されています。");
            }
        }

        // パートナーにメッセージを送信
        public async Task SendMessageToPartner(string message)
        {
            // 送信者のConnectionIdから所属するルームを探す (この実装は簡略化)
            // 本来はOnConnectedAsyncでユーザーとルームを紐付けて管理する
            var groupName = _rooms.FirstOrDefault(r => r.Value.PcConnectionId == Context.ConnectionId || r.Value.PhoneConnectionId == Context.ConnectionId).Key;

            if (groupName != null)
            {
                // 自分以外のルームメンバーにメッセージを送信
                await Clients.OthersInGroup(groupName).SendAsync("ReceiveMessage", message);
            }
        }

        // 接続が切れたときの処理
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var groupName = _rooms.FirstOrDefault(r => r.Value.PcConnectionId == Context.ConnectionId || r.Value.PhoneConnectionId == Context.ConnectionId).Key;
            if (groupName != null)
            {
                // パートナーに切断を通知
                await Clients.OthersInGroup(groupName).SendAsync("PartnerDisconnected");
                // ルーム情報を削除
                _rooms.TryRemove(groupName, out _);
                Console.WriteLine($"[Hub] Client {Context.ConnectionId} disconnected. Room {groupName} closed.");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
