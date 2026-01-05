import os
import sys
import subprocess
import shutil

def run_command(cmd, cwd=None):
    try:
        subprocess.run(cmd, cwd=cwd, check=True, shell=(sys.platform == "win32"))
    except subprocess.CalledProcessError as e:
        print(f"Error executing {cmd}: {e}")
        sys.exit(1)

def main():
    project_root = os.path.dirname(os.path.abspath(__file__))
    src_dir = os.path.join(project_root, "src")
    release_dir = os.path.join(project_root, "release")
    build_dir = os.path.join(project_root, "build")
    
    # Clean previous builds
    if os.path.exists(release_dir):
        shutil.rmtree(release_dir)
    os.makedirs(release_dir)

    print("--- WaifuPaper Build System ---")

    # --- 1. FRONTEND BUILD ---
    print("\n[Frontend] Building Svelte App...")
    frontend_dir = os.path.join(src_dir, "frontend")
    
    # Check if node_modules exists, install if not
    if not os.path.exists(os.path.join(frontend_dir, "node_modules")):
        print("Installing frontend dependencies...")
        run_command(["pnpm", "install"], cwd=frontend_dir)
    
    # Build
    run_command(["pnpm", "build"], cwd=frontend_dir)


    # --- 2. WINDOWS BUILD ---
    print("\n[Windows] Building Executable...")
    win_project_path = os.path.join(src_dir, "windows", "WaifuPaper.csproj")
    
    # Pre-clean source directories
    print("Pre-cleaning windows source directories...")
    dirs_to_clean = [
        os.path.join(src_dir, "windows", "obj"),
        os.path.join(src_dir, "windows", "bin")
    ]
    for d in dirs_to_clean:
        if os.path.exists(d):
            shutil.rmtree(d)

    win_bin_path = os.path.join(build_dir, "windows_bin")
    win_obj_path = os.path.join(build_dir, "windows_obj")
    win_publish_path = os.path.join(build_dir, "windows_publish")

    # Need to make sure the EmbeddedResource path in csproj is correct relative to csproj location
    # Since we moved everything, relative paths like ..\frontend\dist should still work 
    # because src/windows/.. is src/ which contains src/frontend.
    
    cmd = [
        "dotnet", "publish", win_project_path,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained",
        "-p:PublishSingleFile=true",
        "-p:EnableWindowsTargeting=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        f"-p:BaseOutputPath={win_bin_path}/",
        f"-p:BaseIntermediateOutputPath={win_obj_path}/",
        "-o", win_publish_path
    ]
    
    try:
        subprocess.run(cmd, check=True)
        
        # Zip Windows
        win_zip_path = os.path.join(release_dir, "windows")
        shutil.make_archive(win_zip_path, 'zip', win_publish_path)
        print(f"[Windows] Zip created: {win_zip_path}.zip")

    except subprocess.CalledProcessError as e:
        print(f"[Windows] Build failed: {e}")


    # --- 3. LINUX PACKAGING ---
    print("\n[Linux] Packaging...")
    try:
        linux_pkg_dir = os.path.join(build_dir, "linux_pkg")
        if os.path.exists(linux_pkg_dir):
            shutil.rmtree(linux_pkg_dir)
        os.makedirs(linux_pkg_dir)

        # Copy EVERYTHING from src/linux to the root of the package
        linux_src_dir = os.path.join(src_dir, "linux")
        for item in os.listdir(linux_src_dir):
            s = os.path.join(linux_src_dir, item)
            d = os.path.join(linux_pkg_dir, item)
            if os.path.isdir(s):
                shutil.copytree(s, d)
            else:
                shutil.copy2(s, d)
        
        # Copy Frontend Dist to frontend/dist (relative to server.py in root)
        frontend_dist_src = os.path.join(src_dir, "frontend", "dist")
        frontend_dist_dst = os.path.join(linux_pkg_dir, "frontend", "dist")
        shutil.copytree(frontend_dist_src, frontend_dist_dst)

        # Zip Linux
        linux_zip_path = os.path.join(release_dir, "linux")
        shutil.make_archive(linux_zip_path, 'zip', linux_pkg_dir)
        print(f"[Linux] Zip created: {linux_zip_path}.zip")

    except Exception as e:
        print(f"[Linux] Packaging failed: {e}")


    # --- CLEANUP ---
    print("\n[Cleanup] Cleaning up stray source artifacts...")
    stray_dirs = [
        os.path.join(src_dir, "windows", "obj"),
        os.path.join(src_dir, "windows", "bin")
    ]
    for d in stray_dirs:
        if os.path.exists(d):
            shutil.rmtree(d)
            print(f"Removed stray directory: {d}")

    print(f"\nBuild Complete! Releases are in '{release_dir}'")
    print(f"Build artifacts in '{build_dir}'")

if __name__ == "__main__":
    main()