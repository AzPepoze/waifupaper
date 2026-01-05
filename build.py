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

def build_frontend(src_dir):
    print("\n[Frontend] Building Svelte App...")
    frontend_dir = os.path.join(src_dir, "frontend")
    
    if not os.path.exists(os.path.join(frontend_dir, "node_modules")):
        print("Installing frontend dependencies...")
        run_command(["pnpm", "install"], cwd=frontend_dir)
    
    run_command(["pnpm", "build"], cwd=frontend_dir)

    dist_dir = os.path.join(frontend_dir, "dist")
    if not os.path.exists(dist_dir) or not os.listdir(dist_dir):
        print("ERROR: Frontend build failed.")
        sys.exit(1)
    return dist_dir

def build_windows(src_dir, build_dir):
    print("\n[Windows] Building C# Executable...")
    win_project_path = os.path.join(src_dir, "windows", "WaifuPaper.csproj")
    win_bin_path = os.path.join(build_dir, "windows_bin")
    win_obj_path = os.path.join(build_dir, "windows_obj")
    win_publish_path = os.path.join(build_dir, "windows_publish")

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
    run_command(cmd)
    return win_publish_path

def pack_windows(win_publish_path, dist_dir, release_dir, skip_zip=False):
    print("\n[Windows] Preparing assets...")
    win_frontend_dist_dst = os.path.join(win_publish_path, "frontend", "dist")
    if os.path.exists(win_frontend_dist_dst):
        shutil.rmtree(win_frontend_dist_dst)
    shutil.copytree(dist_dir, win_frontend_dist_dst)

    if not skip_zip:
        print("[Windows] Creating zip...")
        win_zip_path = os.path.join(release_dir, "windows")
        shutil.make_archive(win_zip_path, 'zip', win_publish_path)
        print(f"Windows Zip created: {win_zip_path}.zip")

def build_linux(src_dir, build_dir):
    print("\n[Linux] Preparing Build...")
    linux_pkg_dir = os.path.join(build_dir, "linux_pkg")
    if os.path.exists(linux_pkg_dir):
        shutil.rmtree(linux_pkg_dir)
    os.makedirs(linux_pkg_dir)

    linux_src_dir = os.path.join(src_dir, "linux")
    for item in os.listdir(linux_src_dir):
        s = os.path.join(linux_src_dir, item)
        d = os.path.join(linux_pkg_dir, item)
        if os.path.isdir(s):
            shutil.copytree(s, d)
        else:
            shutil.copy2(s, d)
    return linux_pkg_dir

def pack_linux(linux_pkg_dir, dist_dir, release_dir, skip_zip=False):
    print("\n[Linux] Preparing assets...")
    frontend_dist_dst = os.path.join(linux_pkg_dir, "frontend", "dist")
    if os.path.exists(frontend_dist_dst):
        shutil.rmtree(frontend_dist_dst)
    shutil.copytree(dist_dir, frontend_dist_dst)

    if not skip_zip:
        print("[Linux] Creating zip...")
        linux_zip_path = os.path.join(release_dir, "linux")
        shutil.make_archive(linux_zip_path, 'zip', linux_pkg_dir)
        print(f"Linux Zip created: {linux_zip_path}.zip")

def main():
    project_root = os.path.dirname(os.path.abspath(__file__))
    src_dir = os.path.join(project_root, "src")
    release_dir = os.path.join(project_root, "release")
    build_dir = os.path.join(project_root, "build")
    
    no_pack = "--no-pack" in sys.argv

    if os.path.exists(release_dir):
        shutil.rmtree(release_dir)
    os.makedirs(release_dir)

    print("--- WaifuPaper Build System ---")

    # 1. Build Frontend (Shared)
    dist_dir = build_frontend(src_dir)

    # 2. Handle Windows Build/Pack
    try:
        win_publish_path = build_windows(src_dir, build_dir)
        pack_windows(win_publish_path, dist_dir, release_dir, skip_zip=no_pack)
    except Exception as e:
        print(f"Windows build/pack skipped or failed: {e}")

    # 3. Handle Linux Build/Pack
    try:
        linux_pkg_dir = build_linux(src_dir, build_dir)
        pack_linux(linux_pkg_dir, dist_dir, release_dir, skip_zip=no_pack)
    except Exception as e:
        print(f"Linux build/pack skipped or failed: {e}")

    print(f"\nBuild Process Finished. Check '{release_dir}' for outputs.")

if __name__ == "__main__":
    main()