namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Inteface for topic like items.
    /// </summary>
    internal interface ITopicItem
    {
        /// <summary>
        /// The display name of the topic.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The name of the topic to match against subscription topic names.
        /// </summary>
        string FullName { get; }
    }
}