using UdonSharp;
using UnityEngine;
using UdonSharp.Video;
// Skip back / skip forward through the video player's playlist (Claude, 2026-10-04).
// Uses USharpVideo's own playlist calls, so whoever presses it takes control of the player the normal way
// (if the player is locked, only the people allowed to control it can skip).
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraVideoSkip : UdonSharpBehaviour
{
    public USharpVideoPlayer player;
    public void Next() { Skip(1); }
    public void Previous() { Skip(-1); }
    private void Skip(int step)
    {
        if (player == null || player.playlist == null || player.playlist.Length == 0) return;
        int n = player.playlist.Length;
        int current = player.GetPlaylistIndex();
        if (current < 0) current = step > 0 ? -1 : 0;
        int target = ((current + step) % n + n) % n;
        player.TakeOwnership();
        player.SetNextPlaylistVideo(target);
        player.PlayNextVideoFromPlaylist();
    }
}
