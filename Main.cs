using JetBrains.Annotations;
using Life;
using Life.DB;
using Life.InventorySystem;
using Life.Network;
using Life.UI;
using Microsoft.Win32;
using Mirror;
using RTG;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Object581
{
    public class Main : Plugin
    {
        public SQLiteAsyncConnection db { get => LifeDB.db; }

        public Main(IGameAPI api) : base(api) { }

        public override async void OnPluginInit()
        {
            base.OnPluginInit();
            await db.CreateTableAsync<BlockedObject>();
            foreach (var obj in await db.Table<BlockedObject>().ToListAsync())
            {
                var item = Nova.man.item.GetItem(obj.ItemId);
                item.buyable = false;
            }
            Nova.server.OnPlayerReceiveItemEvent += (player, itemId, slotId, number) =>
            {
                ClearBlockedObjets(player);
            };
            new SChatCommand("/object", "Ouvre le menu des objets bloqué", "/object", (player, args) =>
            {
                OpenMenu(player);
            }).Register();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} initialise !");
            Console.ResetColor();
        }

        public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            ClearBlockedObjets(player);
        }

        public async void ClearBlockedObjets(Player player)
        {
            bool remove = false;
            var query = await db.Table<BlockedObject>().ToListAsync();
            for (int i = 0; i < player.setup.inventory.slots; i++)
            {
                var item = player.setup.inventory.items[i];
                if (query.Any(obj => obj.ItemId == item.itemId))
                {
                    var element = query.FirstOrDefault();
                    if (player.account.adminLevel > 0)
                        if (element.BlockedForStaff == false)
                            return;
                    player.setup.inventory.RemoveItem(item.itemId, item.number, true);
                    remove = true;
                }
            }
            if (remove)
                player.Notify("Object581", "Des objets interdit vous ont été retiré.", NotificationManager.Type.Warning);
        }

        public async void OpenMenu(Player  player)
        {
            int select = 0;
            var panel = new UIPanel($"Objets Bloqué", UIPanel.PanelType.TabPrice);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui =>
            {
                select = 0;
                ui.SelectTab();
            });
            panel.AddButton($"<color={LifeServer.COLOR_GREEN}>Ajouter</color>", ui =>
            {
                OpenInputItemIdMenu(player);
            });
            panel.AddButton($"<color={LifeServer.COLOR_RED}>Supprimer</color>", ui =>
            {
                select = 1;
                ui.SelectTab();
            });
            foreach (var obj in await db.Table<BlockedObject>().ToListAsync())
            {
                panel.AddTabLine(player.NewTranslate("Items", Nova.man.item.GetItem(obj.ItemId).itemName), "", GetItemIConId(obj.ItemId), async ui =>
                {
                    switch (select)
                    {
                        case 0:
                            OpenManageMenu(player, obj);
                            break;
                        case 1:
                            await db.DeleteAsync(obj);
                            player.Notify("Object581", "L'Objet est désormais autoriser pour tout le monde.", NotificationManager.Type.Success);
                            player.ClosePanel(ui);
                            OpenMenu(player);
                            break;
                    }
                });
            }
            player.ShowPanelUI(panel);
        }

        public void OpenManageMenu(Player player, BlockedObject blockedObject)
        {
            var panel = new UIPanel("Gestion Objet Bloqué", UIPanel.PanelType.Tab);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddButton("Retour", ui => OpenMenu(player));
            panel.AddTabLine("Autoriser pour les Staffs", async ui =>
            {
                blockedObject.BlockedForStaff = false;
                await db.UpdateAsync(blockedObject);
                player.Notify("Object581", $"L'Objet est désormais autorisé pour les staffs.");
            });
            panel.AddTabLine("Bloquer pour les Staffs", async ui =>
            {
                blockedObject.BlockedForStaff = true;
                await db.UpdateAsync(blockedObject);
                player.Notify("Object581", $"L'Objet est désormais bloqué pour les staffs.");
            });
            player.ShowPanelUI(panel);
        }

        public void OpenInputItemIdMenu(Player player)
        {
            var panel = new UIPanel("Bloquer un Objet", UIPanel.PanelType.Input);
            panel.SetText("Veuillez définir l'id de l'objet a bloquer.");
            panel.SetInputPlaceholder("Id :");
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Valider", async ui =>
            {
                if (!string.IsNullOrEmpty(panel.inputText))
                {
                    if (int.TryParse(panel.inputText, out var value))
                    {
                        var item = Nova.man.item.GetItem(value);
                        if (item != null)
                        {
                            var query = await db.Table<BlockedObject>().Where(obj => obj.ItemId == value).ToListAsync();
                            if (query.Any() == false)
                            {
                                var instance = new BlockedObject();
                                instance.ItemId = value;
                                instance.BlockedForStaff = false;
                                item.buyable = false;
                                await db.InsertAsync(instance);
                                OpenManageMenu(player, instance);
                            }
                            else
                                OpenManageMenu(player, query.FirstOrDefault());
                        }
                        else
                            player.Notify("Object581", "Objet invalide.",  NotificationManager.Type.Error);
                    }
                    else
                        player.Notify("Object581", "Format invalide.", NotificationManager.Type.Error);
                }
                else
                    player.Notify("Object581", "Format invalide.", NotificationManager.Type.Error);
            });
            panel.AddButton("Retour", ui => OpenMenu(player));
            player.ShowPanelUI(panel);
        }

        public static int GetItemIConId(int itemId)
        {
            var item = Nova.man.item.GetItem(itemId);
            if (item == null)
                return -1;
            int iconId = 0;
            if (item is Food)
            {
                var food = item as Food;
                var icon = food.rawSprite;
                iconId = Array.IndexOf(Nova.man.newIcons.ToArray(), icon);
            }
            else
            {
                var icon = item.models.FirstOrDefault(obj => obj?.icon != null).icon;
                iconId = Array.IndexOf(Nova.man.newIcons.ToArray(), icon);
            }
            if (iconId > 0)
                return iconId;
            else
                return -1;
        }
    }
}
