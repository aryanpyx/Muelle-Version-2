//! Notchpyx Dynamic Island integration for Muelle.

use std::{
    env,
    path::PathBuf,
    process::Command,
};
use windows::{
    Win32::{
        Foundation::{LPARAM, WPARAM},
        UI::WindowsAndMessaging::{FindWindowW, PostMessageW, WM_CLOSE},
    },
    core::w,
};

/// Locates the Notchpyx executable (`DynamicBar.exe` or `Notchpyx.exe`).
#[must_use]
pub fn find_executable(custom_path: Option<&str>) -> Option<PathBuf> {
    if let Some(path) = custom_path {
        let p = PathBuf::from(path);
        if p.is_file() {
            return Some(p);
        }
    }

    let local_app_data = env::var_os("LOCALAPPDATA").map(PathBuf::from);
    let user_profile = env::var_os("USERPROFILE").map(PathBuf::from);
    let current_exe_dir = env::current_exe().ok().and_then(|p| p.parent().map(|p| p.to_path_buf()));

    let candidates = [
        current_exe_dir.as_ref().map(|p| p.join(r"notchpyx\DynamicBar.exe")),
        current_exe_dir.as_ref().map(|p| p.join(r"notchpyx\Notchpyx.exe")),
        local_app_data.as_ref().map(|p| p.join(r"Programs\Muelle\notchpyx\DynamicBar.exe")),
        user_profile.as_ref().map(|p| p.join(r"Downloads\Notchpyx_Release\DynamicBar.exe")),
        user_profile.as_ref().map(|p| p.join(r"Downloads\Notchpyx_Release\Notchpyx.exe")),
        local_app_data.as_ref().map(|p| p.join(r"Programs\Notchpyx\DynamicBar.exe")),
        local_app_data.as_ref().map(|p| p.join(r"Programs\Notchpyx\Notchpyx.exe")),
    ];

    for candidate in candidates.into_iter().flatten() {
        if candidate.is_file() {
            return Some(candidate);
        }
    }

    None
}

/// Checks whether Notchpyx is already running.
#[must_use]
pub fn is_running() -> bool {
    // SAFETY: Read-only window lookup; checks DynamicBarIslandStage and FLUTTER_RUNNER_WIN32_WINDOW.
    unsafe {
        FindWindowW(w!("DynamicBarIslandStage"), None).is_ok()
            || FindWindowW(w!("FLUTTER_RUNNER_WIN32_WINDOW"), None).is_ok()
    }
}

/// Ensures Notchpyx is running. Spawns it if absent.
pub fn ensure_running(custom_path: Option<&str>) {
    if is_running() {
        return;
    }

    if let Some(exe) = find_executable(custom_path) {
        if let Some(parent) = exe.parent() {
            let _ = Command::new(&exe)
                .current_dir(parent)
                .spawn();
        }
    }
}

/// Signals Notchpyx to close gracefully when Muelle quits.
pub fn stop() {
    unsafe {
        if let Ok(hwnd) = FindWindowW(w!("DynamicBarIslandStage"), None) {
            let _ = PostMessageW(Some(hwnd), WM_CLOSE, WPARAM(0), LPARAM(0));
        } else if let Ok(hwnd) = FindWindowW(w!("FLUTTER_RUNNER_WIN32_WINDOW"), None) {
            let _ = PostMessageW(Some(hwnd), WM_CLOSE, WPARAM(0), LPARAM(0));
        }
    }
}
