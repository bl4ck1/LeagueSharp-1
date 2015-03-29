using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace GangPlank
{
    internal class Program
    {
        //      {
        private const string ChampName = "Gangplank";
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> Spells = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SpellSlot IgniteSlot;
        private static Obj_AI_Hero _player;

        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }


        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampName)
                return;

            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 1300);            
            R = new Spell(SpellSlot.R);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);
            Spells.Add(R);


            Config = new Menu(ChampName, ChampName, true);


            var tsMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));


            Config.AddSubMenu(new Menu("Combo Settings", "combo"));
            Config.SubMenu("combo").AddItem(new MenuItem("comboKey", "Full Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("combo").AddItem(new MenuItem("autoIgnite", "Use Ignite in combo").SetValue(true));


            Config.AddSubMenu(new Menu("Harass Settings", "harass"));
            Config.SubMenu("harass").AddItem(new MenuItem("autoQ", "Auto-Q when Target in Range").SetValue(new KeyBind("Z".ToCharArray()[0],KeyBindType.Toggle)));
            Config.SubMenu("harass").AddItem(new MenuItem("harassKey", "Harass Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("harass").AddItem(new MenuItem("harassMana", "Min. Mana Percent: ").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Farming Settings", "farm"));
            Config.SubMenu("farm").AddItem(new MenuItem("farmKey", "Farming Key").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("farm").AddItem(new MenuItem("qFarm", "Farm with (Q)").SetValue(true));
            Config.SubMenu("farm").AddItem(new MenuItem("farmMana", "Min. Mana Percent: ").SetValue(new Slider(50)));


            Config.AddSubMenu(new Menu("Misc Settings", "misc"));
            Config.SubMenu("misc").AddItem(new MenuItem("usePackets", "Use Packets to Cast Spells").SetValue(false));
            Config.SubMenu("misc").AddItem(new MenuItem("anticc", "Use Scurvy when stunned/rooted").SetValue(new KeyBind("H".ToCharArray()[0],KeyBindType.Toggle)));
            Config.SubMenu("misc").AddItem(new MenuItem("autoheal", "Use Scurvy to heal yourself").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("misc").AddItem(new MenuItem("autohealhp", "Min Percentage of HP to use W").SetValue(new Slider(40, 1)));
            Config.SubMenu("misc").AddItem(new MenuItem("autohealmana", "Min. Mana for AutoHeal").SetValue(new Slider(40, 1)));
//            Config.SubMenu("misc").AddItem(new MenuItem("ultks", "Ult KS").SetValue(false));

            Config.AddSubMenu(new Menu("Draw Settings", "drawing"));
            Config.SubMenu("drawing").AddItem(new MenuItem("mDraw", "Disable All Range Draws").SetValue(false));
            Config.SubMenu("drawing").AddItem(new MenuItem("Target", "Draw Circle on Target").SetValue(new Circle(true,Color.FromArgb(255, 255, 0, 0))));
            Config.SubMenu("drawing").AddItem(new MenuItem("QDraw", "Draw (Q) Range").SetValue(new Circle(true, Color.FromArgb(255, 178, 0, 0))));


            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;


            Game.PrintChat("<font color=\"#00BFFF\">Daimao GankPlank by Taerarenai -</font> <font color=\"#FFFFFF\">Loaded</font>");
            Game.PrintChat("<font color=\"#00BFFF\">Version:</font> <font color=\"#FFFFFF\">1.0.0.2</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var ultitarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            var comboKey = Config.Item("comboKey").GetValue<KeyBind>().Active;
            var harassKey = Config.Item("harassKey").GetValue<KeyBind>().Active;
            var farmKey = Config.Item("farmKey").GetValue<KeyBind>().Active;
//            var ultks = Config.Item("ultks").GetValue<KeyBind>().Active;
            var autoheal = Config.Item("autoheal").GetValue<KeyBind>().Active;


            if (comboKey && target != null)
                Combo(target);
            else
            {
                if (harassKey && target != null)
                    Harass(target);

                if (farmKey)
                    Farm();

                if (Config.Item("autoQ").GetValue<KeyBind>().Active && target != null &&
                    ObjectManager.Player.Distance(target) <= Q.Range && Q.IsReady())
                    Q.CastOnUnit(target, Config.Item("usePackets").GetValue<bool>());

                if (autoheal)
                    AutoHeal();

                if (Config.Item("anticc").GetValue<KeyBind>().Active && W.IsReady() &&
                    (_player.HasBuffOfType(BuffType.Stun) || _player.HasBuffOfType(BuffType.Fear) ||
                     _player.HasBuffOfType(BuffType.Snare) || _player.HasBuffOfType(BuffType.Taunt)))
                    W.Cast();

                if (Config.Item("ultks").GetValue<KeyBind>().Active && R.IsReady() && ((_player.GetSpellDamage(ultitarget, SpellSlot.R)) > ultitarget.Health))
                R.Cast(ultitarget);
               
            }

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("mDraw").GetValue<bool>())
                return;

            foreach (var spell in Spells.Where(spell => Config.Item(spell.Slot + "Draw").GetValue<Circle>().Active))
            {
                Utility.DrawCircle(ObjectManager.Player.Position, spell.Range,Config.Item(spell.Slot + "Draw").GetValue<Circle>().Color);
            }
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Config.Item("Target").GetValue<Circle>().Active && target != null) Utility.DrawCircle(target.Position, 50, Config.Item("Target").GetValue<Circle>().Color, 1, 50);
        }


        private static void AutoHeal()
        {

            var mana = ObjectManager.Player.MaxMana * (Config.Item("autohealmana").GetValue<Slider>().Value / 100.0);
            if (!(ObjectManager.Player.Mana > mana))
                return;

            var health = ObjectManager.Player.MaxHealth * (Config.Item("autohealhp").GetValue<Slider>().Value / 100.0);
            if (ObjectManager.Player.Health > health)
                return;

            if (W.IsReady())
                W.Cast();
        }

        private static void Combo(Obj_AI_Base target)
        {
            if (target == null)
                return;

            if (E.IsReady())
                E.Cast();

            if (Q.IsReady())
                Q.CastOnUnit(target, Config.Item("usePackets").GetValue<bool>());

            if (!Config.Item("autoIgnite").GetValue<bool>())
                return;

            if (IgniteSlot == SpellSlot.Unknown ||
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) != SpellState.Ready)
                return;

            if (!(ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) >= target.Health))
                return;

            ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, target);
        }


        private static void Harass(Obj_AI_Base target)
        {
            if (target == null)
                return;

            var mana = ObjectManager.Player.MaxMana*(Config.Item("harassMana").GetValue<Slider>().Value/100.0);
            if (!(ObjectManager.Player.Mana > mana))
                return;

            if (Q.IsReady())
                Q.CastOnUnit(target, Config.Item("usePackets").GetValue<bool>());
        }

        private static void Farm()
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var mana = ObjectManager.Player.MaxMana*(Config.Item("farmMana").GetValue<Slider>().Value/100.0);
            if (!(ObjectManager.Player.Mana > mana))
                return;

            if (!Config.Item("qFarm").GetValue<bool>() || !Q.IsReady())
                return;

            foreach (var minion in from minion in minions
                let actualHp =(HealthPrediction.GetHealthPrediction(minion,(int) (ObjectManager.Player.Distance(minion)*1000/1500)) <= minion.MaxHealth*0.15)? ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)*2 : ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) where minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion,(int) (ObjectManager.Player.Distance(minion)*1000/1500)) <= actualHp select minion)
            {
                Q.CastOnUnit(minion, Config.Item("usePackets").GetValue<bool>());
                return;
            }
        }


        private static float ComboDamage(Obj_AI_Base target)
        {
            var dmg = 0d;
            if (Q.IsReady()) dmg += (target.Health <= target.MaxHealth*0.15)? (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q)*2) : ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (W.IsReady()) dmg += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            if (E.IsReady()) dmg += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            if (R.IsReady()) dmg += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
            if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready) dmg += ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            return (float) dmg;
        }
    }
}
