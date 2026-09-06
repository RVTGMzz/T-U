using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int CurfewCheckPulseTicksAlpha6612 = 30;

    private void RegisterAlpha6612Events()
    {
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6612UpdateTicked;
        RegisterAlpha6613Events();
    }

    private void OnAlpha6612UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || Game1.eventUp
            || !e.IsMultipleOf(CurfewCheckPulseTicksAlpha6612)
            || Game1.timeOfDay < 2300)
        {
            return;
        }

        bool changed = false;
        List<Farmer> onlineFarmers = Game1.getOnlineFarmers().ToList();
        if (onlineFarmers.Count == 0)
            onlineFarmers.Add(Game1.player);

        foreach (PartyMemberData member in Party.Members
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .ToList())
        {
            Farmer? recruiter = onlineFarmers.FirstOrDefault(farmer => farmer.UniqueMultiplayerID == member.RecruiterId);
            if (recruiter is null)
                continue;

            int hearts = GetFriendshipHeartsAlpha6612(recruiter, member.CharacterName);
            bool spouse = !string.IsNullOrWhiteSpace(recruiter.spouse)
                && recruiter.spouse.Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase);
            int curfew = GetCurfewTimeAlpha6612(hearts, spouse);
            if (Game1.timeOfDay < curfew)
                continue;

            if (!Party.SetState(member.CharacterName, member.RecruiterId, PartyMemberState.Inactive))
                continue;

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
            {
                ShowCurfewFarewellAlpha6613(npc);
                Follow.ReleaseToVanillaAndResumeSchedule(npc);
            }

            ReleaseLinkedCompanionForCurfewAlpha6612(member);
            changed = true;

            Monitor.Log(
                $"Curfew released {member.CharacterName} at {Game1.timeOfDay} (hearts={hearts}, spouse={spouse}, cutoff={curfew}).",
                LogLevel.Debug);
        }

        if (!changed)
            return;

        SavePartyNow();
        BroadcastPartySnapshot();
    }

    private void ShowCurfewFarewellAlpha6613(NPC npc)
    {
        string line = GetCurfewFarewellAlpha6613(npc.Name);
        if (string.IsNullOrWhiteSpace(line))
            return;

        npc.showTextAboveHead(line, new Color(245, 235, 205), 2, 1900, 0);
    }

    private string GetCurfewFarewellAlpha6613(string characterName)
    {
        bool vi = Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        string name = characterName.Trim();

        if (vi)
        {
            return name.ToLowerInvariant() switch
            {
                "abigail" => "Muộn rồi. Tớ về đây, mai phiêu lưu tiếp nhé!",
                "alex" => "Tớ về nghỉ lấy sức đây. Mai còn phải sung sức chứ!",
                "caroline" => "Khuya rồi đấy. Mình về nhé, bạn cũng nghỉ sớm đi.",
                "clint" => "Ờ... tôi về lò rèn đây. Ngủ ngon nhé.",
                "demetrius" => "Đã đến lúc kết thúc quan sát hôm nay. Hẹn mai nhé.",
                "elliott" => "Đêm đã sâu rồi. Tôi xin phép khép lại cuộc hành trình hôm nay.",
                "emily" => "Năng lượng hôm nay đủ rồi! Mình về nghỉ nhé, ngủ thật vui nha!",
                "evelyn" => "Bà về nghỉ đây, cháu cũng đừng thức khuya quá nhé.",
                "george" => "Đủ rồi. Tôi về ngủ đây. Đừng có lang thang cả đêm đấy.",
                "gus" => "Tôi về đóng bếp đây. Nhớ ăn gì đó rồi hãy ngủ nhé!",
                "haley" => "Muộn quá rồi, tớ phải về chăm sóc da đây. Mai gặp!",
                "harvey" => "Giờ nghỉ ngơi rất quan trọng. Tôi về đây, bạn cũng nên ngủ sớm.",
                "jodi" => "Mình phải về với gia đình rồi. Bạn cẩn thận nhé.",
                "kent" => "Tôi rút về nghỉ. Giữ an toàn cho tới sáng.",
                "leah" => "Tớ về xưởng đây. Đêm yên tĩnh thế này chắc sẽ có ý tưởng hay.",
                "lewis" => "Thị trấn vẫn cần tôi vào sáng mai. Tôi xin phép về trước.",
                "linus" => "Đêm gọi tôi về với lều rồi. Chúc bạn một đêm bình yên.",
                "marnie" => "Mình phải về xem lũ vật nuôi rồi. Mai gặp lại nhé!",
                "maru" => "Tớ về kiểm tra mấy thiết bị rồi ngủ đây. Mai gặp nhé!",
                "pam" => "Tôi nghỉ đây. Mai còn cả đống việc, đừng thức tới sáng đấy!",
                "penny" => "Mình về nhé. Bạn nhớ nghỉ ngơi, đừng cố quá.",
                "pierre" => "Tôi về chuẩn bị cho cửa hàng ngày mai. Hẹn gặp lại!",
                "robin" => "Tớ về nhà đây. Mai còn công trình đang chờ nữa!",
                "sam" => "Tớ chuồn về đây! Mai tiếp tục quẩy nhé!",
                "sandy" => "Mình về trước nha, cưng. Đừng để đêm nuốt mất giấc ngủ đấy!",
                "sebastian" => "Tớ về đây. Đêm nay đủ đông người rồi.",
                "shane" => "Tôi về ngủ. Thế thôi. Mai gặp.",
                "willy" => "Tôi về nghỉ thôi. Mai biển lại gọi từ sớm đấy.",
                "wizard" => "Canh giờ đã đổi. Ta phải trở về tháp trước khi đêm sâu hơn.",
                _ => "Muộn rồi, mình về nghỉ nhé. Mai gặp lại!"
            };
        }

        return name.ToLowerInvariant() switch
        {
            "abigail" => "It's late. I'm heading out. Save some adventure for tomorrow!",
            "alex" => "I'm calling it. Gotta recharge if I'm going to be at my best tomorrow!",
            "caroline" => "It's getting late. I'm heading home, and you should rest too.",
            "clint" => "Uh... I'm heading back to the forge. Good night.",
            "demetrius" => "Today's field observations are complete. See you tomorrow.",
            "elliott" => "The night has grown deep. Time to close today's chapter.",
            "emily" => "That's enough energy for one day! I'm off. Sweet dreams!",
            "evelyn" => "I'm heading home, dear. Don't stay up too late.",
            "george" => "That's enough for tonight. I'm going home. Don't wander around all night.",
            "gus" => "Time to close the kitchen. Get something to eat before bed!",
            "haley" => "It's way too late. I need my beauty sleep. See you tomorrow!",
            "harvey" => "Proper rest matters. I'm heading home, and you should sleep soon too.",
            "jodi" => "I should get back to my family. Take care out here.",
            "kent" => "I'm standing down for the night. Stay safe until morning.",
            "leah" => "I'm heading back to the cabin. Quiet nights are good for ideas.",
            "lewis" => "The town will need me tomorrow morning. I'll head home now.",
            "linus" => "The night is calling me back to my tent. Rest peacefully.",
            "marnie" => "I need to check on the animals before bed. See you tomorrow!",
            "maru" => "I'm heading home to check a few things, then sleep. See you tomorrow!",
            "pam" => "I'm done for tonight. Got plenty to do tomorrow, so don't stay up till sunrise!",
            "penny" => "I'm heading home. Please get some rest too, okay?",
            "pierre" => "I should prepare the shop for tomorrow. See you then!",
            "robin" => "I'm heading home. I've still got a build waiting for me tomorrow!",
            "sam" => "I'm outta here! We'll keep the fun going tomorrow!",
            "sandy" => "I'm heading off, sweetie. Don't let the night steal all your sleep!",
            "sebastian" => "I'm heading home. That's enough people for one night.",
            "shane" => "I'm going home to sleep. That's it. See you tomorrow.",
            "willy" => "Time for me to turn in. The sea calls early tomorrow.",
            "wizard" => "The hour has turned. I must return to the tower before the night deepens.",
            _ => "It's late. I'm heading home to rest. See you tomorrow."
        };
    }

    private void ReleaseLinkedCompanionForCurfewAlpha6612(PartyMemberData member)
    {
        CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is null)
            return;

        Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Standby);

        if (PelipperTownCompatibilityService.IsSourceControlled(linked))
        {
            NPC? actor = PelipperTownCompatibilityService.ResolveActor(linked);
            if (actor is not null)
            {
                PelipperDeploymentStateService.SetDesiredDeployment(
                    actor,
                    member.CharacterName,
                    deployed: false);
            }
            return;
        }

        NPC? linkedNpc = Game1.getCharacterFromName(linked.CharacterName);
        if (linkedNpc is not null)
            Follow.ReleaseToVanilla(linkedNpc);
    }

    private static int GetFriendshipHeartsAlpha6612(Farmer farmer, string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return 0;

        try
        {
            return Math.Clamp(farmer.getFriendshipHeartLevelForNPC(characterName), 0, 14);
        }
        catch
        {
            return 0;
        }
    }

    private static int GetCurfewTimeAlpha6612(int hearts, bool spouse)
    {
        if (spouse || hearts >= 10)
            return 2900;
        if (hearts >= 8)
            return 2600;
        if (hearts >= 6)
            return 2500;
        if (hearts >= 3)
            return 2400;
        return 2300;
    }
}
