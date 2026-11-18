using System.Threading.Tasks;
using Godot;

// Only resources load off-thread. Scene instantiation and entry stay on the main thread.
public static class SceneResourceWarmup
{
    public static async Task<PackedScene> LoadAsync(SceneTree tree, string path)
    {
        Error requested = ResourceLoader.LoadThreadedRequest(path, "PackedScene");
        if (requested != Error.Ok) return null;
        while (true)
        {
            switch (ResourceLoader.LoadThreadedGetStatus(path))
            {
                case ResourceLoader.ThreadLoadStatus.Loaded:
                    return ResourceLoader.LoadThreadedGet(path) as PackedScene;
                case ResourceLoader.ThreadLoadStatus.Failed:
                case ResourceLoader.ThreadLoadStatus.InvalidResource:
                    return null;
            }
            // The tree outlives its menu: finish the request even if that menu closes.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
