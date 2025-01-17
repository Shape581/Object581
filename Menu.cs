using Life;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Life.UI;
using Life.Network;
using Utils;
using System.Net.Mail;
using System.Security.AccessControl;

namespace Object581
{
    public class Menu : Plugin
    {
        public Menu(IGameAPI api) : base(api) { }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Command.Create("/object", "Menu Object581", player =>
            {
                OpenMenu(player);
            });
        }

        public void OpenMenu(Player player)
        {
            var panel = new UIPanel(Format.Color("Object581"), UIPanel.PanelType.Text);
            panel.AddButton(Format.Color("Ajouter", Format.Colors.Green), ui =>
            {
                AddItem(player);
            });
            panel.AddButton(Format.Color("Supprimer", Format.Colors.Red), ui =>
            {
                DeleteItem(player);   
            });
            panel.AddButton("Fermer", ui => player.ClosePanel(panel));
            player.ShowPanelUI(panel);
        }

        public async void DeleteItem(Player player)
        {
            var panel = new UIPanel(Format.Color("Supprimer", Format.Colors.Red), UIPanel.PanelType.TabPrice);
            panel.AddButton("Annuler", ui => OpenMenu(player));
            panel.AddButton(Format.Color("Supprimer", Format.Colors.Red), ui => ui.SelectTab());
            foreach (var elements in await Object.RequestAll())
            {
                var item = Item.GetItem(elements.ItemId);
                panel.AddTabLine(Format.Color($"{item.itemName}"), "", Item.GetIconId(elements.ItemId), async ui =>
                {
                    await elements.Delete();
                    Notify.Send(player, "Vous avez supprimer cette objet.", Notify.Type.Success);
                    OpenMenu(player);
                });
            }
            player.ShowPanelUI(panel);
        }

        public void AddItem(Player player)
        {
            var panel = new UIPanel(Format.Color("Ajout", Format.Colors.Green), UIPanel.PanelType.Input);
            panel.SetText("<b>Ajouter un Objet.</b>");
            panel.SetInputPlaceholder("Entrée l'id de l'item...");
            panel.AddButton("Annuler", ui => OpenMenu(player));
            panel.AddButton(Format.Color("Valider", Format.Colors.Green), async ui =>
            {
                if (int.TryParse(panel.inputText, out int value))
                {
                    var item = Item.GetItem(value);
                    if (item != null)
                    {
                        if (!await Object.Check(obj => obj.ItemId == value))
                        {
                            Object.Write(value);
                            Notify.Send(player, "Vous avez ajouter cette objet.", Notify.Type.Success);
                            OpenMenu(player);
                        }
                        else
                        {
                            Notify.Send(player, "Cette objet est déjà enregistrer.", Notify.Type.Error);
                            player.ClosePanel(panel);
                            AddItem(player);
                        }
                    }
                    else
                    {
                        Notify.Send(player, "Veuillez entrée l'id d'un item existant.", Notify.Type.Error);
                        player.ClosePanel(panel);
                        AddItem(player);
                    }
                }
                else
                {
                    Notify.Send(player, "Veuillez entrée u l'id d'un item valide.", Notify.Type.Error);
                    player.ClosePanel(panel);
                    AddItem(player);
                }
            });
            player.ShowPanelUI(panel);
        }
    }
}
