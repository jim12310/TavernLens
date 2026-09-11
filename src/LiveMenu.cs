using System;using HearthMirror;
namespace TavernLens {
public class LiveMenuSample {public string Mode;public int? Rating;public int Scene;public bool Loaded,Transitioning;}
public static class LiveMenu {
 public static LiveMenuSample Read(){var client=Reflection.Client;var s=client.GetSceneMgrState();if(!s.HasValue)return new LiveMenuSample();var sample=new LiveMenuSample{Scene=s.Value.Mode,Loaded=s.Value.SceneLoaded,Transitioning=s.Value.Transitioning};if(sample.Scene!=15||!sample.Loaded||sample.Transitioning)return sample;
 var mode=client.GetSelectedBattlegroundsGameMode();var rating=client.GetBattlegroundRatingInfo();var after=client.GetSceneMgrState();if(!after.HasValue||after.Value.Mode!=15||!after.Value.SceneLoaded||after.Value.Transitioning||mode!=client.GetSelectedBattlegroundsGameMode())return new LiveMenuSample();
 sample.Mode=mode==HearthMirror.Objects.SelectedBattlegroundsGameMode.SOLO?"Solo":mode==HearthMirror.Objects.SelectedBattlegroundsGameMode.DUOS?"Duos":null;
 if(rating!=null&&sample.Mode!=null){int value=sample.Mode=="Duos"?rating.DuosRating:rating.Rating;if(value>=0&&value<=100000)sample.Rating=value;}return sample;}
}
}
