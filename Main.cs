using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Life;
using Life.DB;
using Life.InventorySystem;
using Life.Network;
using Life.UI;
using Mirror;
using Unity.Profiling.LowLevel.Unsafe;
using Utils;

namespace Object581
{
    public class Main : Plugin
    {
        public Main(IGameAPI api) : base(api) { }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Object.Init();
            Nova.server.OnPlayerReceiveItemEvent += new Action<Player, int, int, int>(Receive);
            Nova.server.OnPlayerDropItemEvent += new Action<Player, int, int, int>(Drop);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Object581] - Intialisé !");
            Console.ResetColor();
        }

        public async void Receive(Player player, int itemId, int slotId, int quantity)
        {
            if (await Object.Check(i => i.ItemId == itemId))
            {
                Utils.Inventory.RemoveItem(player, itemId, quantity);
                var item = Utils.Item.GetItem(itemId);
                Notify.Send(player, $"Vous n'avez pas le droit de posséder l'objet {Format.Color(item.itemName, Format.Colors.Yellow)}, celui ci vous a été retirer.");
            }
        }

        public async void Drop(Player player, int itemId, int slotId, int quantity)
        {
            if (await Object.Check(i => i.ItemId == itemId))
            {
                UnityEngine.Object.FindObjectsOfType<DroppedItem>().ToList().ForEach(droppedItem =>
                {
                    if (droppedItem.itemId == itemId)
                    {
                        NetworkServer.Destroy(droppedItem.gameObject);
                    }
                });
            }
        }

        public override async void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            foreach (var elements in Utils.Inventory.GetPlayerInventory(player))
            {
                int itemId = elements.Key;
                int quantity = elements.Value;
                if (await Object.Check(i => i.ItemId == itemId))
                {
                    Utils.Inventory.RemoveItem(player, itemId, quantity);
                    var item = Utils.Item.GetItem(itemId);
                    Notify.Send(player, $"Vous n'avez pas le droit de posséder l'objet {Format.Color(item.itemName, Format.Colors.Yellow)}, celui ci vous a été retirer.");
                }
            }
        }
    }
}
