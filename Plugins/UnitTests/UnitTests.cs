using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using SharpDX;
using System.Diagnostics;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace UnitTests
{
    partial class UnitTests : BaseSettingsPlugin<UnitTestsSettings>
    {
        public override bool Initialise()
        {
            base.Initialise();
            LogMsg($"Unit Test initialised.");
            try
            {
                GameTests();
                PlayerTests();
                AreaTests();
                LogMsg("All tests passed.");
            } catch(AssertionError err)
            {
                LogMessage(err.Message, 1, Color.Red);
            }
            return true;
        }

        void GameTests()
        {
            var game = GameController.Game;
            Assert("Game is not null", game != null);
            var ingameState = game.IngameState;
            Assert("Game.IngameState is not null", ingameState != null);
            var serverData = ingameState.ServerData;
            Assert("ServerData is not null", serverData != null);
            var playerInventories = serverData.PlayerInventories;
            Assert("PlayerInventories is not null", playerInventories != null);
            foreach(var key in Enum.GetValues(typeof(InventoryTypeE))) {
                Warn($"Has Inventory Type: {key}", playerInventories.Any((x) => x.Inventory.InventType == (InventoryTypeE)key));
            }
            var flaskInventory = playerInventories.FirstOrDefault((x) => x.Inventory != null && x.Inventory.InventType == InventoryTypeE.Flask);
            Assert("Has flasks", flaskInventory != null);
            var slotItems = flaskInventory.Inventory.InventorySlotItems;
            AssertEqual("Has flasks", slotItems.Count, 3);
            foreach(var flask in slotItems)
            {
                Charges charges = null;
                switch (flask.PosX)
                {
                    case 0:
                        Assert("Flask is valid", flask.Item != null);
                        AssertEqual($"Flask [{flask.PosX}] is a life flask", flask.Item.Path, "Metadata/Items/Flasks/FlaskLife1");
                        charges = flask.Item.GetComponent<Charges>();
                        Assert("Flask has Charges component", charges != null);
                        AssertEqual("Flask max charges", charges.ChargesMax, 21);
                        AssertEqual("Flask current charges", charges.NumCharges, 21);
                        AssertEqual("Flask charges per use", charges.ChargesPerUse, 7);
                        break;
                    case 1:
                        Assert("Flask is valid", flask.Item != null);
                        AssertEqual($"Flask [{flask.PosX}] is a life flask", flask.Item.Path, "Metadata/Items/Flasks/FlaskLife1");
                        charges = flask.Item.GetComponent<Charges>();
                        Assert("Flask has Charges component", charges != null);
                        AssertEqual("Flask max charges", charges.ChargesMax, 21);
                        AssertEqual("Flask current charges", charges.NumCharges, 21);
                        AssertEqual("Flask charges per use", charges.ChargesPerUse, 7);
                        break;
                    case 2: break;
                    case 3: break;
                    case 4:
                        Assert("Flask is valid", flask.Item != null);
                        AssertEqual($"Flask [{flask.PosX}] is a mana flask", flask.Item.Path, "Metadata/Items/Flasks/FlaskMana1");
                        charges = flask.Item.GetComponent<Charges>();
                        Assert("Flask has Charges component", charges != null);
                        AssertEqual("Flask max charges", charges.ChargesMax, 24);
                        AssertEqual("Flask current charges", charges.NumCharges, 24);
                        AssertEqual("Flask charges per use", charges.ChargesPerUse, 6);
                        break;
                }
            }

            var mainHand = playerInventories.FirstOrDefault((x) => x.Inventory != null && x.Inventory.InventType == InventoryTypeE.Weapon);
            AssertEqual("Has a weapon equipped", mainHand.Inventory.Items.Count, 1);
            var weapon = mainHand.Inventory.Items[0];
            AssertEqual("Is a sceptre", weapon.Path, "Metadata/Items/Weapons/OneHandWeapons/OneHandMaces/Sceptre1");
            var sockets = weapon.GetComponent<Sockets>();
            Assert("Has sockets", sockets != null);
            AssertEqual("Has 2 linked sockets", sockets.SocketGroup.First(), "RB");
            var mods = weapon.GetComponent<Mods>();
            Assert("Has mods", mods != null);
            AssertEqual("Level 1 item", mods.ItemLevel, 1);
            AssertEqual("Normal rarity", mods.ItemRarity, ItemRarity.Normal);
        }

        void PlayerTests()
        {
            var player = GameController.Player;
            Assert("Player is not null", player != null);
            Assert("Player is alive", player.IsAlive == true);
            var life = player.GetComponent<Life>();
            Assert("Player has a Life component", life != null);
            AssertEqual("Player has life", life.CurHP, 61);
            AssertEqual("Player has mana", life.CurMana, 52);
            AssertEqual("Player has no ES", life.CurES, 0);
        }

        void AreaTests()
        {
            var area = GameController.Area?.CurrentArea;
            Assert("Area is not null", area != null);
            AssertEqual("Running tests in Act 1", area.Act, 1);
            AssertEqual("Is not in town", area.IsTown, false);
            AssertEqual("Is not in hideout", area.IsHideout, false);
        }

        public bool AssertEqual<T>(string label, T a, T b) => Assert($"{label}: got {a}", a.Equals(b));

        public bool Warn(string label, bool value)
        {
            if (!value) { LogMessage($"{label}", 1, Color.Orange); }
            else { LogMessage($"Pass: {label}", 1, Color.Green); }
            return value;
        }
        public bool Assert(string label, bool value)
        {
            if (!value) { throw new AssertionError($"{label}"); }
            else { LogMessage($"Pass: {label}", 1, Color.Green); }
            return value;
        }

        public override Job Tick()
        {
            // TODO: more complicated tests across multiple frames
            return base.Tick();
        }

        public override void Render()
        {
            // TODO: draw some information overlays (flasks, skill buttons, etc)
            // test the UI location data, window size data
            base.Render();
        }

    }
}