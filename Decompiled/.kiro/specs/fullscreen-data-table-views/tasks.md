# Implementation Plan: Fullscreen Data Table Views

## Overview

Replace the current modal dialog pattern (`ShowDialog()`) with in-app fullscreen data table views using a reusable `FullscreenViewHost` UserControl. The implementation leverages the existing `NavigationService`, page caching, and breadcrumb infrastructure. All fullscreen views cover ~85% of the screen area and provide consistent chrome (back button, title, toolbar) across all module pages.

## Tasks

- [x] 1. Create core infrastructure and interfaces
  - [x] 1.1 Create FullscreenViewConfig data model and NavigatingBackEventArgs
    - Create `FullscreenViewConfig.cs` with Title, Subtitle, OriginRoute, Content, ToolbarItems, ShowSideToolbar, OnSaved, and Icon properties
    - Create `NavigatingBackEventArgs.cs` with Cancel, HasUnsavedChanges, OriginRoute, and RefreshOnReturn properties
    - Add validation logic: Title must be non-empty, Content must be non-null, OriginRoute must be non-empty
    - _Requirements: 1.2, 1.3_

  - [x] 1.2 Create IRefreshable interface
    - Create `IRefreshable.cs` interface with `RefreshData()` method
    - This interface will be implemented by module pages that need to refresh data when returning from a fullscreen view
    - _Requirements: 2.6_

  - [x] 1.3 Create FullscreenFormBase abstract class
    - Create `FullscreenFormBase.cs` as an abstract UserControl base class
    - Implement IsDirty and IsValid properties with change notification
    - Define abstract methods: `ValidateForm()`, `SaveAsync()`, `ResetForm()`
    - Implement `TrySaveAsync()` with validation-before-save logic, loading state, and error handling
    - Implement `ConfirmDiscard()` method for unsaved changes confirmation
    - Fire `DirtyStateChanged` and `SaveCompleted` events appropriately
    - _Requirements: 3.1, 3.5, 6.1, 6.2, 6.3, 6.4, 6.6_

  - [ ]* 1.4 Write unit tests for FullscreenViewConfig validation
    - Test that null/empty Title throws ArgumentException
    - Test that null Content throws ArgumentNullException
    - Test that empty OriginRoute throws ArgumentException
    - Test valid config passes validation
    - _Requirements: 1.2, 1.3_

  - [ ]* 1.5 Write property test for Config Validation Rejects Invalid Input
    - **Property 2: Config Validation Rejects Invalid Input**
    - **Validates: Requirements 1.2, 1.3**

- [x] 2. Implement FullscreenViewHost UserControl
  - [x] 2.1 Create FullscreenViewHost XAML layout
    - Create `FullscreenViewHost.xaml` UserControl with the following layout structure:
      - Outer Border with CornerRadius 14 and CardStyle DropShadowEffect
      - Header row: Back button (44×44 min), optional Icon, ViewTitle, ViewSubtitle
      - Content presenter area occupying 80-90% of available height
      - Top toolbar panel (horizontal) and side toolbar panel (vertical, conditional)
    - Use Slate color palette from existing theme
    - _Requirements: 1.4, 2.1, 5.1, 5.2, 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 2.2 Create FullscreenViewHost code-behind with dependency properties
    - Define DependencyProperties: ViewTitle, ViewSubtitle, OriginRoute, ContentArea, ToolbarItems, ShowSideToolbar
    - Implement `NavigateBack()` method with unsaved changes guard
    - Implement `SetUnsavedChanges(bool)` method
    - Wire NavigatingBack and BackCompleted events
    - Handle conditional visibility for subtitle and icon elements
    - _Requirements: 1.5, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4_

  - [x] 2.3 Implement toolbar rendering and accessibility
    - Render toolbar items in horizontal top bar or vertical side panel based on ShowSideToolbar
    - Set AutomationProperties.Name on each toolbar button matching visible label text
    - Ensure Tab navigation order (left-to-right or top-to-bottom)
    - Ensure buttons are activatable via Enter or Space keys
    - Implement overflow menu when more than 10 buttons in horizontal mode
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 2.4 Implement permission-based toolbar filtering
    - Check `Permissions.Has(permissionKey)` for each toolbar action button
    - Hide buttons for which the user lacks the corresponding permission
    - Show read-only indicator when all toolbar actions are hidden
    - _Requirements: 10.3, 10.4_

  - [ ]* 2.5 Write property test for Layout Constraint
    - **Property 7: Layout Constraint**
    - **Validates: Requirement 1.4**

  - [ ]* 2.6 Write property test for Toolbar Accessibility
    - **Property 8: Toolbar Accessibility**
    - **Validates: Requirements 5.3, 5.4**

  - [ ]* 2.7 Write property test for Permission-Based Toolbar Filtering
    - **Property 12: Permission-Based Toolbar Filtering**
    - **Validates: Requirement 10.3**

- [x] 3. Implement FullscreenViewStyles ResourceDictionary
  - [x] 3.1 Create FullscreenViewStyles.xaml ResourceDictionary
    - Define styles: FullscreenHostBorderStyle, FullscreenBackButtonStyle, FullscreenTitleStyle, FullscreenSubtitleStyle, FullscreenToolbarPanelStyle, FullscreenSideToolbarStyle, FullscreenContentPresenterStyle, FullscreenSaveButtonStyle, FullscreenCancelButtonStyle
    - Use existing Slate palette, CornerRadius 14, CardStyle DropShadowEffect (BlurRadius 16, ShadowDepth 2, Opacity 0.06)
    - Include entrance animation storyboard (complete within 350ms) and exit animation (within 200ms)
    - _Requirements: 7.3, 9.1, 9.2_

  - [x] 3.2 Register FullscreenViewStyles in App.xaml merged dictionaries
    - Add the ResourceDictionary reference to App.xaml so styles are available application-wide
    - _Requirements: 9.1_

- [x] 4. Implement Navigation Extensions and Debouncing
  - [x] 4.1 Create FullscreenNavigationExtensions static class
    - Implement `NavigateToFullscreen(this NavigationService nav, FullscreenViewConfig config)` extension method
    - Validate config (Title, Content, OriginRoute)
    - Create and configure FullscreenViewHost from config
    - Push navigation history entry for breadcrumb support
    - Wire BackCompleted event to NavigateBackFromFullscreen
    - Store OnSaved callback for post-save invocation
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 4.3_

  - [x] 4.2 Implement NavigateBackFromFullscreen method
    - Retrieve cached origin page via NavigatePage(originRoute)
    - If refreshOnReturn is true, invoke IRefreshable.RefreshData() on the restored page
    - Handle cache miss by recreating page via GetOrCreate factory
    - _Requirements: 2.2, 2.4, 2.5, 2.6_

  - [x] 4.3 Implement navigation debouncing logic
    - Add 200ms debounce window to NavigationService for navigation requests
    - Ignore subsequent navigation calls while a transition animation is in progress
    - Reset debounce state if the first request fails
    - Accept new requests normally after debounce window elapses and no animation is running
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 4.4 Implement single active view constraint
    - Ensure pageHost.Content is set to exactly one view at all times
    - Remove previous view from visual tree before new view becomes visible
    - Complete or cancel in-progress transitions before applying new navigation
    - Retain current view if target view fails to instantiate
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [ ]* 4.5 Write property test for Navigation Round-Trip Integrity
    - **Property 1: Navigation Round-Trip Integrity**
    - **Validates: Requirements 2.2, 2.3**

  - [ ]* 4.6 Write property test for Cache Idempotency
    - **Property 4: Cache Idempotency**
    - **Validates: Requirements 7.1, 7.2**

  - [ ]* 4.7 Write property test for Navigation Debouncing
    - **Property 11: Navigation Debouncing**
    - **Validates: Requirements 8.1, 8.2**

  - [ ]* 4.8 Write property test for Single Active View Invariant
    - **Property 6: Single Active View Invariant**
    - **Validates: Requirements 11.1, 11.2**

- [x] 5. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement Breadcrumb Trail Integration
  - [x] 6.1 Update breadcrumb display for fullscreen views
    - When navigating to fullscreen: set breadcrumb to "OriginTitle › ViewTitle"
    - When navigating back: revert breadcrumb to "Home › ModuleTitle"
    - Truncate ViewTitle with ellipsis if exceeding 50 characters, set ToolTip with full title
    - Limit breadcrumb depth to two segments for nested fullscreen views
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]* 6.2 Write property test for Breadcrumb Round-Trip
    - **Property 5: Breadcrumb Round-Trip**
    - **Validates: Requirements 4.1, 4.2, 4.3**

- [x] 7. Implement Unsaved Changes Protection
  - [x] 7.1 Implement unsaved changes guard in FullscreenViewHost
    - Check if ContentArea is FullscreenFormBase and IsDirty is true
    - Show confirmation dialog with "Discard Changes" and "Keep Editing" options
    - If discard: proceed with navigation, reset Dirty_State
    - If cancel: dismiss dialog, preserve all form state
    - Intercept sidebar menu clicks and keyboard shortcuts when dirty
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [ ]* 7.2 Write property test for Unsaved Changes Guard
    - **Property 3: Unsaved Changes Guard**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4**

- [x] 8. Implement Form Save Workflow
  - [x] 8.1 Implement save workflow in FullscreenFormBase
    - Disable save button on activation
    - Call ValidateForm() before SaveAsync()
    - On validation failure: show FormValidationPanel errors, re-enable save button
    - On save success: clear validation messages, set IsDirty to false, fire SaveCompleted, show success toast, invoke OnSaved callback
    - On save failure/timeout (30s): show error toast, re-enable save button, preserve form input
    - Prevent duplicate submissions while save is in progress
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [ ]* 8.2 Write property test for Validate Before Persist
    - **Property 13: Validate Before Persist**
    - **Validates: Requirement 6.1**

  - [ ]* 8.3 Write property test for Save Failure Preserves Form State
    - **Property 9: Save Failure Preserves Form State**
    - **Validates: Requirements 6.2, 6.4**

  - [ ]* 8.4 Write property test for Successful Save State Transition
    - **Property 10: Successful Save State Transition**
    - **Validates: Requirements 6.3, 6.5**

- [x] 9. Implement Page Caching and Performance
  - [x] 9.1 Implement LRU cache eviction in NavigationService
    - Enforce maximum of 30 cached page instances
    - Evict least-recently-accessed page when limit is reached
    - Ensure fullscreen view origin pages are not evicted during active fullscreen sessions
    - _Requirements: 7.1, 7.2, 7.7_

  - [x] 9.2 Implement async data loading with LoadingOverlay
    - Load Content_Panel data asynchronously after view transition completes
    - Display LoadingOverlay during data fetch
    - Hide LoadingOverlay and show error with retry option if fetch fails or exceeds 30 seconds
    - Enable UI virtualization on DataGrids with more than 100 rows
    - _Requirements: 7.4, 7.5, 7.6_

  - [ ]* 9.3 Write property test for Refresh on Return
    - **Property 14: Refresh on Return**
    - **Validates: Requirement 2.5**

- [x] 10. Implement Security Integration
  - [x] 10.1 Implement session lock handling for fullscreen views
    - Retain all unsaved form field values during session lock
    - Restore focus to previously active field after re-authentication
    - Discard in-memory form data and navigate to login on logout from LockScreenWindow
    - _Requirements: 10.1, 10.2_

  - [x] 10.2 Implement form input validation with FormValidationPanel
    - Validate all user input fields using existing FormValidationPanel control
    - Prevent form submission when validation errors are present
    - Display validation errors inline
    - _Requirements: 10.5_

- [x] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Migrate first module page (ResidentModulePage)
  - [x] 12.1 Convert ResidentModulePage Add/Edit/View actions to fullscreen views
    - Replace `ShowDialog()` calls with `NavigateToFullscreen()` calls
    - Create `ResidentFormPanel` as a FullscreenFormBase subclass for Add/Edit
    - Create `ResidentDetailPanel` for View mode with side toolbar
    - Wire Save button to TrySaveAsync with NavigateBackFromFullscreen on success
    - Implement IRefreshable on ResidentModulePage
    - _Requirements: 1.1, 2.6, 5.7, 6.3, 6.5_

  - [ ]* 12.2 Write integration tests for ResidentModulePage fullscreen navigation
    - Test: Open ResidentModulePage → Click Add → Verify fullscreen view appears
    - Test: Fill form → Save → Verify return to module page with data refreshed
    - Test: Back navigation preserves module page state
    - _Requirements: 1.1, 2.2, 2.4_

- [x] 13. Migrate remaining module pages
  - [x] 13.1 Convert BlotterPage actions to fullscreen views
    - Replace ShowDialog() calls with NavigateToFullscreen() for Add/Edit/View/Resolve
    - Create BlotterFormPanel as FullscreenFormBase subclass
    - Wire toolbar actions (Save, Resolve Case)
    - Implement IRefreshable on BlotterPage
    - _Requirements: 1.1, 5.7_

  - [x] 13.2 Convert CertificatePage actions to fullscreen views
    - Replace ShowDialog() calls with NavigateToFullscreen() for Issue/View certificates
    - Create CertificateFormPanel as FullscreenFormBase subclass
    - Implement IRefreshable on CertificatePage
    - _Requirements: 1.1_

  - [x] 13.3 Convert remaining module pages to fullscreen views
    - Apply the same pattern to all other module pages (Household, Officials, Announcements, etc.)
    - Each module page: replace ShowDialog() → NavigateToFullscreen(), implement IRefreshable
    - Ensure consistent toolbar configuration per module
    - _Requirements: 1.1_

- [x] 14. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document using FsCheck
- Unit tests validate specific examples and edge cases
- The migration tasks (12, 13) follow the pattern established in the infrastructure tasks (1-10)
- All fullscreen views reuse the same FullscreenViewHost container for visual consistency

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "3.1"] },
    { "id": 1, "tasks": ["1.3", "3.2"] },
    { "id": 2, "tasks": ["1.4", "1.5", "2.1"] },
    { "id": 3, "tasks": ["2.2", "2.3"] },
    { "id": 4, "tasks": ["2.4", "2.5", "2.6", "2.7"] },
    { "id": 5, "tasks": ["4.1", "4.3", "4.4"] },
    { "id": 6, "tasks": ["4.2", "4.5", "4.6", "4.7", "4.8"] },
    { "id": 7, "tasks": ["6.1"] },
    { "id": 8, "tasks": ["6.2", "7.1"] },
    { "id": 9, "tasks": ["7.2", "8.1"] },
    { "id": 10, "tasks": ["8.2", "8.3", "8.4", "9.1"] },
    { "id": 11, "tasks": ["9.2", "9.3", "10.1", "10.2"] },
    { "id": 12, "tasks": ["12.1"] },
    { "id": 13, "tasks": ["12.2", "13.1"] },
    { "id": 14, "tasks": ["13.2", "13.3"] }
  ]
}
```
