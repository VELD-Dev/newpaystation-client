using Spectre.Console;
using Spectre.Console.Rendering;

namespace NoPayStationClient.UI.Pages
{
    public abstract class BasePage : IPage
    {
        protected bool _shouldExit = false;

        public abstract IRenderable Render();

        public abstract Task<bool> HandleInputAsync(ConsoleKeyInfo? key);

        public virtual void OnEnter()
        {
            _shouldExit = false;
        }

        public virtual void OnExit()
        {
        }

        public virtual int RefreshIntervalMs => 100;

        protected IRenderable CreateHeader(string title)
        {
            return new Rule($"[cyan]{title}[/]")
            {
                Justification = Justify.Left
            };
        }
    }
}
