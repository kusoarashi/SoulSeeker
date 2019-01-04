using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;

namespace SoulSeeker
{
    [WireUpBaseEvents]

    [MVView("SoulSeeker.mainView.xml")]
    [MVWireUpControlEvents]

    [FriendlyName("SoulSeeker")]
    public class PluginCore : PluginBase
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        const UInt32 WM_CLOSE = 0x0010;

        private static Timer DelayLogout = new Timer();
        private static Timer RelogTimer = new Timer()
        {
            Interval = 1000
        };

        private static Random rand = new Random();

        private SoundPlayer Ally = new SoundPlayer();
        private SoundPlayer Enemy = new SoundPlayer();

        private string MyAppPath = "";
        private static bool PKLogout;
        private static bool PKAlert;
        private static bool AltF4;
        private static bool DeathLogout;
        private static bool FellowTarget;
        private static bool FellowLeader;
        private static bool PlaySounds;
        private static bool Vitae;
        private static bool LowComps;
        private static bool relog;
        private static bool friendly;
        private static int MyMonarch;
        private static string[] friends;
        private static string[] options;
        private static string[] monarchy;

        private Dictionary<string, bool> SoulSeekerOptions = new Dictionary<string, bool>();
        private Dictionary<int, int> Friends = new Dictionary<int, int>();

        [MVControlReference("PlayerList")]
        private IList PlayerList = null;

        [MVControlReference("NewFriend")]
        private IStaticText NewFriendBox = null;

        [MVControlReference("LogoutCheckBox")]
        private ICheckBox LogoutCheckBox = null;

        [MVControlReference("AlertCheckBox")]
        private ICheckBox AlertCheckBox = null;

        [MVControlReference("ALTF4CheckBox")]
        private ICheckBox AltF4CheckBox = null;

        [MVControlReference("DeathLogoutCheckBox")]
        private ICheckBox DeathLogoutCheckBox = null;

        [MVControlReference("FellowTargetCheckBox")]
        private ICheckBox FellowTargetCheckBox = null;

        [MVControlReference("SoundsCheckBox")]
        private ICheckBox SoundsCheckBox = null;

        [MVControlReference("VitaeCheckBox")]
        private ICheckBox VitaeCheckBox = null;

        [MVControlReference("CompsCheckBox")]
        private ICheckBox CompsCheckBox = null;

        protected override void Startup()
        {
            try
            {
                Globals.Init("SoulSeeker", Host, Core);

                MVWireupHelper.WireupStart(this, Host);

                MyAppPath = Assembly.GetExecutingAssembly().Location.ToString();

                int num = MyAppPath.LastIndexOf('\\');

                if (num > 0)
                {
                    MyAppPath = MyAppPath.Substring(0, num + 1);
                }

                Ally.SoundLocation = MyAppPath.Trim() + "Ally.wav";
                Ally.LoadAsync();
                Enemy.SoundLocation = MyAppPath.Trim() + "Enemy.wav";
                Enemy.LoadAsync();

                DelayLogout.Tick += DoLogout;
                RelogTimer.Tick += DoRelogout;

                relog = true;
                FellowLeader = false;
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        protected override void Shutdown()
        {
            try
            {
                MVWireupHelper.WireupEnd(this);
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("Login", "CharacterFilter")]
        private void CharacterFilter_Login(object sender, LoginEventArgs e)
        {
            Core.Actions.AddChatText("-=[SoulSeeker : v9.1.1 : By Blacksoul]=-", 13);

            string filename = MyAppPath.Trim() + "monarch.cfg";

            try
            {
                monarchy = File.ReadAllLines(filename);
            }
            catch (Exception) { Util.WriteToChat("Missing or empty monarch.cfg"); }

            foreach (string monarch in monarchy)
            {
                MyMonarch = Convert.ToInt32(monarch);
            }

            filename = MyAppPath.Trim() + Core.CharacterFilter.Name.Trim().ToLower() + ".cfg";

            if (File.Exists(filename))
                Util.WriteToChat("Settings loaded from: " + Core.CharacterFilter.Name.Trim().ToLower() + ".cfg");
            else
            {
                filename = MyAppPath.Trim() + "default.cfg";
                Util.WriteToChat("Defaut settings loaded.");
            }

            try
            {
                options = File.ReadAllLines(filename);
            }
            catch (Exception) { Util.WriteToChat("Empty or missing config files."); }

            foreach (string option in options)
            {
                string[] optionHashes = option.Split('=');

                switch (optionHashes[0])
                {
                    case "logout":
                        if (optionHashes[1] == "true")
                        {
                            PKLogout = true;
                            LogoutCheckBox.Checked = true;
                        }
                        else
                        {
                            PKLogout = false;
                            LogoutCheckBox.Checked = false;
                        }
                        break;
                    case "alert":
                        if (optionHashes[1] == "true")
                        {
                            PKAlert = true;
                            AlertCheckBox.Checked = true;
                        }
                        else
                        {
                            PKAlert = false;
                            AlertCheckBox.Checked = false;
                        }
                        break;
                    case "altF4":
                        if (optionHashes[1] == "true")
                        {
                            AltF4 = true;
                            AltF4CheckBox.Checked = true;
                        }
                        else
                        {
                            AltF4 = false;
                            AltF4CheckBox.Checked = false;
                        }
                        break;
                    case "death":
                        if (optionHashes[1] == "true")
                        {
                            DeathLogout = true;
                            DeathLogoutCheckBox.Checked = true;
                        }
                        else
                        {
                            DeathLogout = false;
                            DeathLogoutCheckBox.Checked = false;
                        }
                        break;
                    case "sound":
                        if (optionHashes[1] == "true")
                        {
                            PlaySounds = true;
                            SoundsCheckBox.Checked = true;
                        }
                        else
                        {
                            PlaySounds = false;
                            SoundsCheckBox.Checked = false;
                        }
                        break;
                    case "comps":
                        if (optionHashes[1] == "true")
                        {
                            LowComps = true;
                            CompsCheckBox.Checked = true;
                        }
                        else
                        {
                            LowComps = false;
                            CompsCheckBox.Checked = false;
                        }
                        break;
                    case "vitae":
                        if (optionHashes[1] == "true")
                        {
                            Vitae = true;
                            VitaeCheckBox.Checked = true;

                            if ((Vitae == true) && (Core.CharacterFilter.Vitae >= 10))
                            {
                                DelayLogout.Interval = NewRand();
                                DelayLogout.Start();
                            }
                        }
                        else
                        {
                            Vitae = false;
                            VitaeCheckBox.Checked = false;
                        }
                        break;
                }
            }

            filename = MyAppPath.Trim() + "friends.cfg";

            try
            {
                friends = File.ReadAllLines(filename);
            }
            catch (Exception) { Util.WriteToChat("Missing or empty friends.cfg"); }

            foreach (string friend in friends)
            {
                int friendID = Convert.ToInt32(friend);
                
                if (int.TryParse(friend, out int friendID)) //thx again parad0x
                    Friends.Add(friendID, friendID);
            }

            try
            {
                Core.EchoFilter.ServerDispatch += new EventHandler<NetworkMessageEventArgs>(EchoFilter_ServerDispatch);
                //Core.EchoFilter.ClientDispatch += new EventHandler<NetworkMessageEventArgs>(EchoFilter_ClientDispatch);
                //Core.CommandLineText += new EventHandler<ChatParserInterceptEventArgs>(Core_CommandLineText);
                Core.WorldFilter.ChangeObject += new EventHandler<ChangeObjectEventArgs>(WorldFilter_ChangeObject);
                Core.WorldFilter.CreateObject += new EventHandler<CreateObjectEventArgs>(WorldFilter_CreateObject);
                Core.WorldFilter.ReleaseObject += new EventHandler<ReleaseObjectEventArgs>(WorldFilter_ReleaseObject);
                Core.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Core_ChatBoxMessage);
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        /*
        [BaseEvent("LoginComplete", "CharacterFilter")]
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
        }
        */

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                Core.EchoFilter.ServerDispatch -= new EventHandler<NetworkMessageEventArgs>(EchoFilter_ServerDispatch);
                //Core.EchoFilter.ClientDispatch -= new EventHandler<NetworkMessageEventArgs>(EchoFilter_ClientDispatch);
                //Core.CommandLineText -= new EventHandler<ChatParserInterceptEventArgs>(Core_CommandLineText);
                Core.WorldFilter.ChangeObject -= new EventHandler<ChangeObjectEventArgs>(WorldFilter_ChangeObject);
                Core.WorldFilter.CreateObject -= new EventHandler<CreateObjectEventArgs>(WorldFilter_CreateObject);
                Core.WorldFilter.ReleaseObject -= new EventHandler<ReleaseObjectEventArgs>(WorldFilter_ReleaseObject);
                Core.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Core_ChatBoxMessage);
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private int NewRand()
        {
            int r = rand.Next(500, 800);
            return r;
        }

        private void DoRelogout(object sender, EventArgs e)
        {
            RelogTimer.Stop();
            RelogTimer.Dispose();
            Host.Actions.Logout();
        }

        private void DoLogout(object sender, EventArgs e)
        {
            DelayLogout.Stop();
            DelayLogout.Dispose();

            if (AltF4 == true)
            {
                IntPtr windowPtr = FindWindowByCaption(IntPtr.Zero, Core.CharacterFilter.Name);

                if (windowPtr == IntPtr.Zero)
                {
                    string filename = MyAppPath.Trim() + "errors.txt";
                    using (StreamWriter sw = new StreamWriter(@filename, true))
                        sw.WriteLine("Window named '" + Core.CharacterFilter.Name + "' not found! Attemping regular logout...");
                    Util.WriteToChat("Window named '" + Core.CharacterFilter.Name + "' not found! Attemping regular logout...");
                    Host.Actions.Logout();
                }
                else
                    SendMessage(windowPtr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            else
                Host.Actions.Logout();
        }

        private string ConvertMonarch(int monarchID)
        {
            int MonarchID = monarchID;
            string str;

            switch (MonarchID)
            {
                case 1342194028:
                    str = "Hookin and Cookin";
                    break;
                case 1342184788:
                    str = "Charmin";
                    break;
                case 1342179702:
                    str = "Get Down";
                    break;
                case 0:
                    str = "-no monarch-";
                    break;
                default:
                    str = MonarchID.ToString();
                    Host.Actions.AddChatText("-=[Monarchy : " + str + " name not in database.]=-", 5);
                    break;
            }

            return str;
        }

        private void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            if (e.Message.Type == 0x019E)
                if (e.Message["killed"].Equals(Core.CharacterFilter.Id))
                {
                    if (DeathLogout == true)
                    {
                        DelayLogout.Interval = NewRand();
                        DelayLogout.Start();
                    }
                    else if (Vitae == true)
                    {
                        if (Core.CharacterFilter.Vitae >= 10)
                        {
                            DelayLogout.Interval = NewRand();
                            DelayLogout.Start();
                        }
                    }
                }
        }

        private void Core_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            if ((e.Text.Trim().ToString() == "Logging off...") && (relog == true))
            {
                relog = false;
                RelogTimer.Start();
            }

            if (e.Text.Contains("You have created the Fellowship") || e.Text.Contains("You are now the leader of your Fellowship"))
                FellowLeader = true;

            if (e.Text.Contains("is now the leader of your Fellowship."))
                FellowLeader = false;

            if (e.Text.Contains("[Seek]:") && (FellowTarget == true) && (FellowLeader == false))
            {
                string text1 = e.Text;
                int num1 = text1.LastIndexOf(" ");
                int CalledTargetGUID = (Convert.ToInt32(text1.Substring(num1 + 1, 10)));
                string str = CalledTargetGUID.ToString();

                if (e.Text.Contains("-"))
                    Host.Actions.SelectItem(Convert.ToInt32(text1.Substring(num1 + 1, 11)));
                else
                    Host.Actions.SelectItem(Convert.ToInt32(text1.Substring(num1 + 1, 10)));

                //Host.Actions.CastSpell(0x709, Host.Actions.CurrentSelection);
            }
        }

        private void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e)
        {
            WorldObject changed = e.Changed;
            if ((LowComps == true) && (changed.ObjectClass == ObjectClass.SpellComponent))
            {
                int[] comps = new int[9];

                foreach (WorldObject worldObject in Core.WorldFilter.GetByObjectClass(ObjectClass.SpellComponent))
                {
                    if (worldObject.Name == "Prismatic Taper")
                        comps[0] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Lead Scarab")
                        comps[1] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Iron Scarab")
                        comps[2] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Copper Scarab")
                        comps[3] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Silver Scarab")
                        comps[4] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Gold Scarab")
                        comps[5] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Pyreal Scarab")
                        comps[6] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Platinum Scarab")
                        comps[7] += worldObject.Values(LongValueKey.StackCount);

                    if (worldObject.Name == "Mana Scarab")
                        comps[8] += worldObject.Values(LongValueKey.StackCount);
                }

                foreach (int comp in comps)
                {
                    if ((comp != 0) && (comp <= 5))
                    {
                        DelayLogout.Interval = NewRand();
                        DelayLogout.Start();
                    }
                }
            }
        }

        private void WorldFilter_ReleaseObject(object sender, ReleaseObjectEventArgs e)
        {
            WorldObject released = e.Released;
            if (released.ObjectClass == ObjectClass.Player)
            {
                if (released.Id != Core.CharacterFilter.Id)
                {
                    PlayerListRemove(released.Id);

                    int monarchID = released.Values(LongValueKey.Monarch);
                    string coords = released.Coordinates().ToString();

                    if (monarchID == MyMonarch)
                        friendly = true;
                    else if (Friends.ContainsKey(released.Id))
                        friendly = true;
                    else
                        friendly = false;

                    if (friendly == true)
                        Host.Actions.AddChatText("-=[Ally: (" + released.Name + ") released to: " + coords + "]=-", 13);
                    else if (friendly == false)
                        Host.Actions.AddChatText("-=[Enemy: (" + released.Name + ") released to: " + coords + "]=-", 10);
                }
            }
        }

        private void WorldFilter_CreateObject(object sender, CreateObjectEventArgs e)
        {
            WorldObject worldObject = e.New;
            if (worldObject.ObjectClass == ObjectClass.Player)
            {
                if (worldObject.Id != Core.CharacterFilter.Id)
                {
                    int monarchID = worldObject.Values(LongValueKey.Monarch);
                    string coords = worldObject.Coordinates().ToString();

                    if (monarchID == MyMonarch)
                        friendly = true;
                    else if (Friends.ContainsKey(worldObject.Id))
                        friendly = true;
                    else
                        friendly = false;

                    if (friendly == false)
                    {
                        if (PKAlert == true)
                        {
                            PlayerListAdd(worldObject.Name, monarchID, worldObject.Id, Core.CharacterFilter.Name, coords);
                            Host.Actions.InvokeChatParser("/a -=[Enemy: (" + worldObject.Name + ") detected at: " + coords + "!]=-");

                            if (PlaySounds == true)
                                Enemy.Play();
                        }
                        else
                        {
                            PlayerListAdd(worldObject.Name, monarchID, worldObject.Id, Core.CharacterFilter.Name, coords);
                            Host.Actions.AddChatText("-=[Enemy: (" + worldObject.Name + ") detected at: " + coords + "]=-", 10);

                            if (PlaySounds == true)
                                Enemy.Play();
                        }

                        if (PKLogout == true)
                        {
                            DelayLogout.Interval = NewRand();
                            DelayLogout.Start();
                        }
                    }
                    else if (friendly == true)
                    {
                        Host.Actions.AddChatText("-=[Ally: (" + worldObject.Name + ") detected at: " + coords + "]=-", 13);
                        if (PlaySounds == true)
                            Ally.Play();
                    }
                }
            }
        }

        private void PlayerListAdd(string playerName, int playerMonarch, int playerID, string detectorName, string coordinates)
        {
            int MonarchID = playerMonarch;
            string str = ConvertMonarch(MonarchID);

            IListRow listRow = PlayerList.Add();

            listRow[0][0] = playerName;
            listRow[1][0] = str;
            listRow[2][0] = playerID.ToString();

            string filename = MyAppPath.Trim() + "pklog.txt";

            using (StreamWriter sw = new StreamWriter(@filename, true))
                sw.WriteLine(detectorName + " detected: [" + playerName + " : " + playerID.ToString() + "] - [Monarch : " + playerMonarch.ToString() + "] - [Location: " + coordinates + "] - " + DateTime.Now.ToString());
        }

        private void PlayerListRemove(int playerID)
        {
            for (int index = 0; index < PlayerList.RowCount; ++index)
            {
                if ((string)PlayerList[index][2][0] == playerID.ToString())
                {
                    PlayerList.Delete(index);
                    break;
                }
            }
        }

        [MVControlEvent("PlayerList", "Selected")]
        private void SampleList_Selected(object sender, MVListSelectEventArgs e)
        {
            Host.Actions.SelectItem(Convert.ToInt32(PlayerList[e.Row][2][0]));
            NewFriendBox.Text = Host.Actions.CurrentSelection.ToString();

            if (FellowTarget == true && FellowLeader == true)
            {
                Host.Actions.InvokeChatParser("/f [Seek]: " + Host.Actions.CurrentSelection.ToString() + " [Name]: " + PlayerList[e.Row][1][0]);
                //Host.Actions.CastSpell(0x709, Host.Actions.CurrentSelection);
            }
        }

        [MVControlEvent("AddFriend", "Click")]
        void SaveFriends(object sender, MVControlEventArgs e)
        {
            try
            {
                int NewFriendly = Convert.ToInt32(NewFriendBox.Text.Trim());

                if (!Friends.TryGetValue(NewFriendly, out int IsFriendly))
                {
                    PlayerListRemove(NewFriendly);
                    Friends.Add(NewFriendly, NewFriendly);

                    try
                    {
                        string filename = MyAppPath.Trim() + "friends.cfg";
                        using (StreamWriter sw = new StreamWriter(@filename, true))
                            sw.WriteLine(NewFriendBox.Text);
                    }
                    catch (Exception ex) { Util.LogError(ex); }

                    NewFriendBox.Text = null;

                    Util.WriteToChat("New friendly added!");
                }
                else
                    Util.WriteToChat("Oops... " + IsFriendly.ToString() + " might already be in friends.cfg?");
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Invalid integer? Check the log.");
                Util.LogError(ex);
            }
        }

        [MVControlEvent("LogoutCheckBox", "Change")]
        void LogoutCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (LogoutCheckBox.Checked == true)
                {
                    PKLogout = true;
                    Util.WriteToChat("Logout ENABLED");
                }
                else if (LogoutCheckBox.Checked == false)
                {
                    PKLogout = false;
                    AltF4 = false;
                    Util.WriteToChat("Logout DISABLED");
                    AltF4CheckBox.Checked = false;
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("AlertCheckBox", "Change")]
        void AlertCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (AlertCheckBox.Checked == true)
                {
                    PKAlert = true;
                    Util.WriteToChat("PK Alert ENABLED");
                }
                else if (AlertCheckBox.Checked == false)
                {
                    PKAlert = false;
                    Util.WriteToChat("PK Alert DISABLED");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("ALTF4CheckBox", "Change")]
        void AltF4CheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (AltF4CheckBox.Checked == true)
                {
                    AltF4 = true;
                    Util.WriteToChat("Alt+F4 logout ENABLED");
                }
                else if (AltF4CheckBox.Checked == false)
                {
                    AltF4 = false;
                    Util.WriteToChat("Alt+F4 Logout DISABLED (using in-game logout)");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("DeathLogoutCheckBox", "Change")]
        void DeathLogoutCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (DeathLogoutCheckBox.Checked == true)
                {
                    DeathLogout = true;
                    Util.WriteToChat("Logout on death ENABLED");
                }
                else if (DeathLogoutCheckBox.Checked == false)
                {
                    DeathLogout = false;
                    Util.WriteToChat("Logout on death DISABLED");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("FellowTargetCheckBox", "Change")]
        void FellowTargetCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (FellowTargetCheckBox.Checked == true)
                {
                    FellowTarget = true;
                    Util.WriteToChat("Fellowship target control ENABLED");
                }
                else if (FellowTargetCheckBox.Checked == false)
                {
                    FellowTarget = false;
                    Util.WriteToChat("Fellowship target control DISABLED");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("SoundsCheckBox", "Change")]
        void SoundsCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (SoundsCheckBox.Checked == true)
                {
                    PlaySounds = true;
                    Util.WriteToChat("Play Sounds ENABLED");
                }
                else if (SoundsCheckBox.Checked == false)
                {
                    PlaySounds = false;
                    Util.WriteToChat("Play Sounds DISABLED");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("VitaeCheckBox", "Change")]
        void VitaeCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (VitaeCheckBox.Checked == true)
                {
                    Vitae = true;
                    Util.WriteToChat("Vitae Logout ENABLED");
                }
                else if (VitaeCheckBox.Checked == false)
                {
                    Vitae = true;
                    Util.WriteToChat("Vitae Logout DISABLED");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("CompsCheckBox", "Change")]
        void CompsCheckBox_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                if (CompsCheckBox.Checked == true)
                {
                    LowComps = true;
                    Util.WriteToChat("Low Comps Logout ENABLED");
                }
                else if (CompsCheckBox.Checked == false)
                {
                    LowComps = true;
                    Util.WriteToChat("Low Comps Logout DISABLED");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("SaveButton", "Click")]
        void SaveConfig(object sender, MVControlEventArgs e)
        {
            try
            {
                string filename = MyAppPath.Trim() + Core.CharacterFilter.Name.Trim().ToLower() + ".cfg";
                StreamWriter sw = new StreamWriter(filename);
                sw.WriteLine("logout=" + PKLogout.ToString().ToLower());
                sw.WriteLine("death=" + DeathLogout.ToString().ToLower());
                sw.WriteLine("vitae=" + Vitae.ToString().ToLower());
                sw.WriteLine("comps=" + LowComps.ToString().ToLower());
                sw.WriteLine("alert=" + PKAlert.ToString().ToLower());
                sw.WriteLine("altF4=" + AltF4.ToString().ToLower());
                sw.WriteLine("sound=" + PlaySounds.ToString().ToLower());
                sw.Close();
            }
            catch (Exception ex) { Util.LogError(ex); }

            Util.WriteToChat("Saved to: " + Core.CharacterFilter.Name.Trim().ToLower() + ".cfg");
        }

        /*
        [MVControlEvent("FImp", "Click")]
        void Cast_Imperil(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("FVuln", "Click")]
        void Cast_Vuln(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("FArc", "Click")]
        void Cast_Arc(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("FBolt", "Click")]
        void Cast_Bolt(object sender, MVControlEventArgs e)
        {
        }

        [MVControlEvent("FStreak", "Click")]
        void Cast_Streak(object sender, MVControlEventArgs e)
        {
        }
        */
    }
}
