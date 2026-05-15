# Requirements Document

## Introduction

This document defines the requirements for replacing the current modal dialog pattern (separate Window instances opened via `ShowDialog()`) with in-app fullscreen data table views in the Barangay Management System. The fullscreen views cover approximately 85% of the screen area and provide a more integrated, less disruptive user experience for data entry and detail viewing across all module pages.

## Glossary

- **Fullscreen_View_Host**: A reusable container UserControl that provides consistent chrome (back button, title, toolbar) and hosts module-specific content panels within approximately 85% of the available screen area.
- **Module_Page**: Any of the existing module pages (ResidentModulePage, BlotterPage, CertificatePage, etc.) that serve as the origin point for fullscreen view navigation.
- **Navigation_Service**: The existing singleton service responsible for page navigation, page caching, and content host management.
- **Navigation_History**: The existing component that tracks navigation entries for breadcrumb trail and back/forward support.
- **Content_Panel**: The module-specific form or detail view displayed inside the Fullscreen_View_Host content area.
- **Fullscreen_Form_Base**: An abstract base class for content panels that provides form infrastructure including validation, dirty tracking, and save/cancel operations.
- **Origin_Route**: The route key identifying the module page from which the user navigated to a fullscreen view.
- **Toolbar**: A collection of action buttons (Save, Resolve, Edit, etc.) displayed either horizontally at the top or vertically on the side of the fullscreen view.
- **Dirty_State**: A boolean flag indicating whether the user has made unsaved modifications to form data.
- **Breadcrumb_Trail**: The visual navigation path indicator showing the user's current location in the application hierarchy.

## Requirements

### Requirement 1: Fullscreen View Navigation

**User Story:** As a barangay staff member, I want to open data entry forms as fullscreen views within the application, so that I can work without disruptive modal dialog windows.

#### Acceptance Criteria

1. WHEN a user clicks an action button (Add, Edit, View) on a Module_Page, THE Navigation_Service SHALL navigate to a Fullscreen_View_Host containing the relevant Content_Panel instead of opening a separate Window via ShowDialog().
2. WHEN navigating to a fullscreen view, THE Navigation_Service SHALL validate that the FullscreenViewConfig contains a non-empty Title (at least 1 character), a non-null Content, and an Origin_Route that matches a registered route in the application's route table before proceeding.
3. IF the FullscreenViewConfig contains a null Content or empty Title, THEN THE Navigation_Service SHALL cancel the navigation, throw an ArgumentException in Debug builds, and display a toast notification indicating the invalid configuration in Release builds without crashing.
4. WHEN a fullscreen view is opened, THE Fullscreen_View_Host SHALL display the Content_Panel occupying 80% to 90% of the main window's client area height.
5. WHEN a user activates the back or close control within the Fullscreen_View_Host, THE Navigation_Service SHALL navigate back to the Origin_Route specified in the FullscreenViewConfig and restore the previous Module_Page state within 300 milliseconds.
6. IF a fullscreen view is already displayed and the user triggers another action button, THEN THE Navigation_Service SHALL replace the current Fullscreen_View_Host content with the new Content_Panel and push the previous Origin_Route onto the navigation history.

### Requirement 2: Back Navigation

**User Story:** As a barangay staff member, I want to return to the originating module page from a fullscreen view, so that I can continue browsing records after completing a data entry task.

#### Acceptance Criteria

1. THE Fullscreen_View_Host SHALL display a fixed-position Back button with a minimum tap target of 44×44 pixels that remains visible at the top of the viewport regardless of scroll position.
2. WHEN the user clicks the Back button and no unsaved changes exist, THE Navigation_Service SHALL navigate back to the Origin_Route module page within 500 milliseconds.
3. IF the user clicks the Back button and unsaved changes exist, THEN THE Fullscreen_View_Host SHALL display a confirmation dialog indicating that unsaved changes will be lost, with options to discard changes and navigate back or cancel and remain on the current view.
4. WHEN the user navigates back to the Origin_Route, THE Navigation_Service SHALL restore the cached Module_Page instance preserving its DataGrid scroll position, row selection state, and active filter state.
5. IF the origin page is no longer in the Navigation_Service cache, THEN THE Navigation_Service SHALL recreate the page using the standard GetOrCreate factory method.
6. WHEN back navigation completes with a refresh flag set to true, THE Module_Page SHALL refresh its data by invoking the IRefreshable.RefreshData() method.

### Requirement 3: Unsaved Changes Protection

**User Story:** As a barangay staff member, I want to be warned before losing unsaved form data, so that I do not accidentally discard my work.

#### Acceptance Criteria

1. WHILE a Content_Panel has Dirty_State equal to true, WHEN the user activates the Back button, THE Fullscreen_View_Host SHALL present a confirmation dialog with a discard option and a cancel option before allowing back navigation.
2. WHILE a Content_Panel has Dirty_State equal to true, WHEN the user attempts to navigate away via sidebar menu clicks or keyboard shortcuts, THE Fullscreen_View_Host SHALL present the same confirmation dialog before allowing navigation.
3. WHEN the user selects the discard option in the confirmation dialog, THE Navigation_Service SHALL proceed with back navigation, reset the Content_Panel Dirty_State to false, and release the form without persisting changes.
4. WHEN the user selects the cancel option in the confirmation dialog, THE Fullscreen_View_Host SHALL dismiss the dialog and remain active with all form field values, scroll positions, and selection states preserved.
5. IF the Content_Panel does not implement dirty state tracking, THEN THE Fullscreen_View_Host SHALL allow navigation without presenting a confirmation dialog.

### Requirement 4: Breadcrumb Trail Integration

**User Story:** As a barangay staff member, I want to see my current navigation location in the breadcrumb trail, so that I understand where I am in the application hierarchy.

#### Acceptance Criteria

1. WHEN a fullscreen view is opened, THE Breadcrumb_Trail SHALL display the path by setting the root label to the OriginTitle and the current label to the ViewTitle, separated by the "›" delimiter.
2. WHEN the user navigates back to the Module_Page, THE Breadcrumb_Trail SHALL revert to display "Home" as the root label and the ModuleTitle as the current label.
3. WHEN navigating to a fullscreen view, THE Navigation_History SHALL record a new entry containing the fullscreen route key and the ViewTitle so that the breadcrumb can render the current path.
4. IF the ViewTitle exceeds 50 characters, THEN THE Breadcrumb_Trail SHALL truncate the displayed text with an ellipsis and set a ToolTip containing the full title.
5. IF a fullscreen view is opened from another fullscreen view, THEN THE Breadcrumb_Trail SHALL display only the immediate origin title and the current ViewTitle, maintaining a maximum depth of two segments.

### Requirement 5: Toolbar Actions

**User Story:** As a barangay staff member, I want action buttons (Save, Resolve, Print) available in the fullscreen view, so that I can perform operations on the displayed data without additional navigation.

#### Acceptance Criteria

1. THE Fullscreen_View_Host SHALL render Toolbar items in a horizontal top bar when the ShowSideToolbar option is not enabled, displaying a maximum of 10 action buttons before requiring an overflow menu.
2. WHERE the ShowSideToolbar option is enabled, THE Fullscreen_View_Host SHALL render Toolbar items in a vertical side panel in the same order as they would appear in the horizontal top bar.
3. THE Fullscreen_View_Host SHALL ensure all Toolbar action buttons are keyboard-accessible via Tab navigation in left-to-right order (or top-to-bottom in side panel mode) and activatable via Enter or Space keys.
4. THE Fullscreen_View_Host SHALL set AutomationProperties.Name on each Toolbar action button to a value that describes the button's action (matching the button's visible label text).
5. IF a Toolbar action cannot be performed on the currently displayed data, THEN THE Fullscreen_View_Host SHALL render the corresponding action button in a disabled state and prevent activation.
6. WHEN a Toolbar action button is activated, THE Fullscreen_View_Host SHALL display a visible status indication within 1 second confirming whether the operation succeeded or failed.
7. THE Fullscreen_View_Host SHALL include at minimum the Save, Resolve, and Print action buttons in the Toolbar when the displayed data supports those operations.

### Requirement 6: Form Validation and Save

**User Story:** As a barangay staff member, I want form validation feedback and reliable save operations, so that I can submit correct data and be confident it is persisted.

#### Acceptance Criteria

1. WHEN the user triggers a save action, THE Fullscreen_Form_Base SHALL disable the save control and invoke ValidateForm() before attempting to persist data.
2. IF ValidateForm() returns false, THEN THE Content_Panel SHALL display all validation error messages using the FormValidationPanel control, re-enable the save control, and remain on the current form without navigating away.
3. WHEN ValidateForm() returns true and SaveAsync() succeeds within 30 seconds, THE Fullscreen_Form_Base SHALL clear any previously displayed FormValidationPanel messages, set IsDirty to false, fire the SaveCompleted event, re-enable the save control, and display a success toast notification via ToastNotification.
4. IF SaveAsync() throws an exception or does not complete within 30 seconds, THEN THE Fullscreen_Form_Base SHALL display an error toast notification via ToastNotification indicating the failure reason, re-enable the save control, and preserve all user input in the form.
5. WHEN a save operation completes successfully and an OnSaved callback is configured, THE Navigation_Service SHALL invoke the OnSaved callback to allow the origin page to refresh.
6. WHILE a save operation is in progress, THE Fullscreen_Form_Base SHALL keep the save control disabled to prevent duplicate submissions.

### Requirement 7: Page Caching and Performance

**User Story:** As a barangay staff member, I want instant transitions between module pages and fullscreen views, so that the application feels responsive and does not interrupt my workflow.

#### Acceptance Criteria

1. WHEN navigating to a fullscreen view, THE Navigation_Service SHALL retain the origin Module_Page instance in its page cache so that returning to the same route key does not recreate the page.
2. THE Navigation_Service SHALL return the same cached UIElement instance for a given route key across multiple GetOrCreate calls, provided neither InvalidateCache for that key nor ClearCache has been invoked since the instance was created.
3. WHEN a fullscreen view transition occurs, THE Fullscreen_View_Host SHALL complete its entrance animation within 350 milliseconds and its exit animation within 200 milliseconds.
4. WHEN a Content_Panel contains a DataGrid expected to display more than 100 rows, THE Content_Panel SHALL enable UI virtualization on the items panel so that only visible rows are rendered in memory.
5. THE Fullscreen_View_Host SHALL load Content_Panel data asynchronously after the view transition completes, displaying the LoadingOverlay during the data fetch.
6. IF an asynchronous data fetch initiated by the Fullscreen_View_Host fails or exceeds 30 seconds, THEN THE Fullscreen_View_Host SHALL hide the LoadingOverlay and display an error message indicating the failure reason with an option to retry.
7. THE Navigation_Service page cache SHALL hold a maximum of 30 page instances; WHEN this limit is reached and a new page is requested, THE Navigation_Service SHALL evict the least-recently-accessed cached page before caching the new one.

### Requirement 8: Concurrent Navigation Debouncing

**User Story:** As a barangay staff member, I want the application to handle rapid clicks gracefully, so that accidental double-clicks do not cause navigation errors.

#### Acceptance Criteria

1. WHEN multiple navigation requests occur within 200 milliseconds, THE Navigation_Service SHALL process only the first request and discard subsequent ones without displaying an error message or visual indicator to the user.
2. WHILE a view transition animation is in progress, THE Navigation_Service SHALL queue no additional navigation requests and SHALL accept new navigation requests only after the current transition animation has completed.
3. IF the first navigation request within a debounce window fails, THEN THE Navigation_Service SHALL reset the debounce state and accept the next navigation request immediately.
4. WHEN the 200-millisecond debounce window elapses and no transition animation is in progress, THE Navigation_Service SHALL accept the next navigation request normally.

### Requirement 9: Visual Consistency

**User Story:** As a barangay staff member, I want all fullscreen views to look and behave consistently, so that I can learn the interface once and apply that knowledge across all modules.

#### Acceptance Criteria

1. THE Fullscreen_View_Host SHALL apply styles from the FullscreenViewStyles ResourceDictionary for consistent visual appearance across all modules.
2. THE Fullscreen_View_Host SHALL use the existing Slate color palette, a CornerRadius of 14 for the outer container, and the CardStyle DropShadowEffect (BlurRadius 16, ShadowDepth 2, Opacity 0.06) defined in the application theme.
3. THE Fullscreen_View_Host SHALL display a header area arranged as a single horizontal row in the following left-to-right order: Back button, optional icon, ViewTitle, and ViewSubtitle.
4. IF the ViewSubtitle property is null or empty, THEN THE Fullscreen_View_Host SHALL hide the subtitle element and display only the Back button, optional icon, and ViewTitle without additional whitespace for the missing subtitle.
5. IF the optional icon is not provided, THEN THE Fullscreen_View_Host SHALL collapse the icon area and display the ViewTitle immediately after the Back button without additional whitespace for the missing icon.

### Requirement 10: Security Integration

**User Story:** As a system administrator, I want fullscreen views to respect session security and role-based permissions, so that unauthorized actions are prevented and data is protected.

#### Acceptance Criteria

1. WHILE the session is locked due to inactivity during an active fullscreen view, THE system SHALL retain all unsaved form field values in the current view's controls and restore focus to the previously active field after successful re-authentication via the LockScreenWindow.
2. IF the user chooses to log out from the LockScreenWindow instead of re-authenticating, THEN THE system SHALL discard the in-memory form data and navigate to the login screen without submitting any pending changes.
3. THE Fullscreen_View_Host SHALL render Toolbar action buttons only for actions where Permissions.Has(permissionKey) returns true for the current user's role, hiding buttons for which the user lacks the corresponding permission key.
4. IF the user's role does not grant permission for any Toolbar action in the fullscreen view, THEN THE Fullscreen_View_Host SHALL display the Toolbar with all action buttons hidden and show a read-only indicator to the user.
5. THE Content_Panel SHALL validate all user input fields using the existing FormValidationPanel control to display validation errors, and SHALL prevent form submission when one or more validation errors are present.

### Requirement 11: Single Active View Constraint

**User Story:** As a developer, I want the system to enforce that only one view is active at a time, so that the application state remains predictable and consistent.

#### Acceptance Criteria

1. THE Navigation_Service SHALL ensure that exactly one view (either a Module_Page or a Fullscreen_View_Host) is displayed in the pageHost ContentControl at any point in time, with zero other views rendered in the visual tree of pageHost.
2. WHEN a new view is navigated to, THE Navigation_Service SHALL set the pageHost.Content property to the new view instance, removing the previous view from the visual tree before the new view becomes visible.
3. IF a navigation request is received while a navigation transition is already in progress, THEN THE Navigation_Service SHALL complete or cancel the in-progress transition before applying the new navigation, ensuring that no more than one view is ever assigned to pageHost.Content.
4. IF the target view fails to instantiate during navigation, THEN THE Navigation_Service SHALL retain the current view in pageHost.Content unchanged and report the failure via an error indication.
