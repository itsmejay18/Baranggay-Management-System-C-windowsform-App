using System.Threading;
using System.Threading.Tasks;

namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Interface for Content_Panel controls that support asynchronous data loading.
/// When a content panel implements this interface, the FullscreenViewHost will
/// automatically invoke LoadContentAsync after the view transition completes,
/// displaying a LoadingOverlay during the fetch.
///
/// Requirements: 7.4, 7.5, 7.6
/// </summary>
public interface IAsyncContentLoader
{
    /// <summary>
    /// Loads the content panel's data asynchronously.
    /// Called by FullscreenViewHost after the view transition animation completes.
    /// The LoadingOverlay is displayed while this method is executing.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that is cancelled if the loading exceeds 30 seconds
    /// or if the user navigates away before loading completes.
    /// </param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadContentAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user-friendly message to display in the LoadingOverlay while data is loading.
    /// Example: "Loading residents..." or "Fetching case details..."
    /// </summary>
    string LoadingMessage { get; }
}
