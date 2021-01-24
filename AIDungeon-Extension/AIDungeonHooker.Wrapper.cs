using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDungeon_Extension.Core
{
    public class AIDungeonWrapper
    {
        //featureFlags
        public class FeatureFlag
        {
            public string id;
            public string name;
            public bool active;
            public string value;
            public string __typename; //FeatureFlag
        }

        public class User
        {
            public string id;
            public string username;
            public string email;
            public string verifiedAt;
            public string hasPremium;
            public string publicId;
            public string shouldPromptReview;
            public string onFirstAdventure;
            public string avatar;
            public object isDeveloper;
            public bool enforceEnergy;
            public int newNotificationCount;
            public string accessToken;
            public object continueAdventure;
            public GameSettings gameSettings;
            public string __typename; //User

            public class GameSettings
            {
                public string id;
                public string displayTheme;
                public string displayColors;
                public int webActionWindowSize;
                public int mobileActionWindowSize;
                public string defaultMode;
                public bool showTips;
                public bool showCommands;
                public bool showModes;
                public object hiddenCommands;
                public object showIconText;
                public string proofRead;
                public string adventureDisplayMode;
                public string textFont;
                public int textSize;
                public int textSpeed;
                public bool playMusic;
                public bool playNarration;
                public int musicVolume;
                public int narrationVolume;
                public bool nsfwGeneration;
                public bool unrestrictedInput;
                public bool actionScoreOptIn;
                public string energyBarDisplayMode;
                public string energyBarAppearance;

                public string __typename; //GameSettings
            }

            public class PremiumSubscription
            {
                public string id;
                public string status;

                public string __typename; //PremiumSubscription
            }

            public class Energy
            {
                public string id;
                public int currentEnergy;
                public string increasedAt;

                public string __typename; //Energy
            }
        }

        public class Adventure
        {
            public List<Action> actionWindow;
            public List<Action> undoneWindow;
            public string id;
            public string playPublicId;
            public string publicId;
            public bool thirdPerson;
            public int actionCount;
            public int playerCount;
            public bool hasNewMessages;
            public string type;
            public int score;
            public Action lastAction;
            public bool actionLoading;
            public object error;
            public object gameState;
            public object events;
            public string message;
            public string userId;
            public List<Player> allPlayers;
            public string mode;
            public object worldId;
            public bool safeMode;
            public object stats;
            public bool died;
            public List<Quest> quests;
            public string memory;
            public object authorsNote;
            public object music;

            public string __typename; //Adventure

            public override string ToString()
            {
                return string.Format("({0}/{1}/{2})", id, publicId, type);
            }

            public class Quest
            {
                public string id;
                public string text;
                public bool completed;
                public bool active;
                public string actionGainedId;
                public string actionCompletedId;

                public string __typename; //Quest
            }
            public class Player
            {
                public string id;
                public string userId;
                public object characterName;
                public object isTyping;
                public string lastTypingAt;

                public string __typename; //Player
            }
        }
        public class Scenario
        {
            public string memory;
            public string id;
            public string prompt;
            public string publicId;
            public List<Option> options;

            public string __typename; //Scenario
            public class Option
            {
                public string id;
                public string publicId;
                public string title;

                public string __typename; //Scenario
            }
        }
        public class Action : IComparer<Action>, IComparable<Action>
        {
            public string id;
            public string text;
            public string type;
            public string adventureId;
            public string undoneAt;
            public string deletedAt;
            public DateTime createdAt;

            public string __typename; //Action

            public int CompareTo(Action other)
            {
                if (other == null)
                    return 0;

                return createdAt.CompareTo(other.createdAt);
            }

            public int Compare(Action x, Action y)
            {
                return x.CompareTo(y);
            }

            public override string ToString()
            {
                return string.Format("({0}/{1}/{2})", id, type, text);
            }
        }
    }
}
