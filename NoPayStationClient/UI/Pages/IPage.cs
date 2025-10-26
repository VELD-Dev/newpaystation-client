using Spectre.Console.Rendering;

namespace NewPayStation.Client.UI.Pages
{
    public interface IPage
    {
        /// <summary>
        /// Renders the page content
        /// </summary>
        IRenderable Render();

        /// <summary>
        /// Handles keyboard input for the page
        /// </summary>
        /// <returns>True if should exit this page</returns>
        Task<bool> HandleInputAsync(ConsoleKeyInfo? key);

        /// <summary>
        /// Called when the page is first shown
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called when the page is exited
        /// </summary>
        void OnExit();

        /// <summary>
        /// Refresh interval in milliseconds (default 100ms)
        /// </summary>
        int RefreshIntervalMs => 100;
    }
}
