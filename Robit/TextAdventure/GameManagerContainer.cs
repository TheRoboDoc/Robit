using DSharpPlus.Entities;

namespace Robit.TextAdventure
{
    /// <summary>
    /// A container class to contain text based adventure game instances.
    /// Also provides methods to add, remove, and search those instances.
    /// </summary>
    public class GameManagerContainer
    {
        private List<GameManager> managers;

        public GameManagerContainer()
        {
            managers = new List<GameManager>();
        }

        /// <summary>
        /// Adds a game manager instance to the container
        /// </summary>
        /// <param name="gameManager">Game manager to add</param>
        public void AddManager(GameManager gameManager) { managers.Add(gameManager); }

        /// <summary>
        /// Removes a game manager instance from the container
        /// </summary>
        /// <param name="gameManager">Game manager to remove</param>
        public void RemoveManager(GameManager gameManager) { managers.Remove(gameManager); }

        /// <summary>
        /// Gets all of the game manager instances in the container
        /// </summary>
        /// <returns>A list of all the game managers in the container</returns>
        public List<GameManager> GetManagers() { return managers; }

        /// <summary>
        /// Gets a game manager instance via text base adventure games name
        /// </summary>
        /// <param name="gameName">Name to search by</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// Manager found: Returns the game manager intance
        /// </item>
        /// <item>
        /// Manager not found: Returns <c>null</c>
        /// </item>
        /// </list>
        /// </returns>
        public GameManager? GetManagerByName(string gameName) { return managers.Find(gameManager => gameManager.gameName == gameName); }

        /// <summary>
        /// Gets a game manager instance via the thread it is run at
        /// </summary>
        /// <param name="threadChannel">Thread to search by</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// Manager found: Returns the game manager intance
        /// </item>
        /// <item>
        /// Manager not found: Returns <c>null</c>
        /// </item>
        /// </list>
        /// </returns>
        public GameManager? GetManagerByThread(DiscordThreadChannel threadChannel) 
        { 
            return managers.Find(gameManager => gameManager.channel == threadChannel); 
        }
    }
}
