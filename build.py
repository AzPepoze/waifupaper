import os
import sys
import subprocess
import shutil
import concurrent.futures
import time
from threading import Lock

import json

print_lock = Lock()


def safe_print(msg):
    with print_lock:
        print(msg)


def load_config(src_dir):
    config_path = os.path.join(src_dir, "config.json")
    if os.path.exists(config_path):
        try:
            with open(config_path, "r") as f:
                return json.load(f)
        except:
            pass
    return {}


def run_command(cmd, cwd=None, name=""):
    try:
        if "dotnet" in cmd:
            cmd.extend(["-p:EnableWindowsTargeting=true"])
        subprocess.run(cmd, cwd=cwd, check=True, shell=(sys.platform == "win32"), stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
    except subprocess.CalledProcessError as e:
        err_msg = e.stderr.decode() if e.stderr else "Unknown error"
        safe_print(f"[ERROR] {name} failed: {err_msg}")
        os._exit(1)


def build_frontend(src_dir):
    safe_print("[Frontend] Building Svelte App...")
    start_time = time.time()
    frontend_dir = os.path.join(src_dir, "frontend")
    if not os.path.exists(os.path.join(frontend_dir, "node_modules")):
        run_command(["pnpm", "install"], cwd=frontend_dir, name="Frontend Install")
    run_command(["pnpm", "build"], cwd=frontend_dir, name="Frontend Build")
    safe_print(f"[Frontend] Done in {time.time() - start_time:.2f}s")
    return os.path.join(frontend_dir, "dist")


def build_windows_component(name, project_path, build_dir, assembly_name):
    start_time = time.time()
    safe_print(f"[Windows][{name}] Building as {assembly_name}...")

    temp_publish = os.path.join(build_dir, f"win_{name}_pub")
    if os.path.exists(temp_publish):
        shutil.rmtree(temp_publish)

    proj_build_bin = os.path.join(build_dir, f"win_{name}_bin")
    proj_build_obj = os.path.join(build_dir, f"win_{name}_obj")

    cmd = [
        "dotnet",
        "publish",
        project_path,
        "-c",
        "Release",
        "-r",
        "win-x64",
        "--self-contained",
        "false",
        "-p:PublishSingleFile=false",
        "-p:EnableWindowsTargeting=true",
        f"-p:AssemblyName={assembly_name}",
        f"-p:BaseOutputPath={proj_build_bin}/",
        f"-p:BaseIntermediateOutputPath={proj_build_obj}/",
        "-o",
        temp_publish,
    ]

    run_command(cmd, name=f"Windows {name}")
    safe_print(f"[Windows][{name}] Finished in {time.time() - start_time:.2f}s")
    return temp_publish


def cleanup_windows_files(path):
    cultures = ["cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant"]
    for culture in cultures:
        culture_path = os.path.join(path, culture)
        if os.path.isdir(culture_path):
            shutil.rmtree(culture_path)

    for item in os.listdir(path):
        if item.endswith(".pdb") or item.endswith(".xml") or item.endswith(".deps.json"):
            file_path = os.path.join(path, item)
            if os.path.isfile(file_path):
                os.remove(file_path)

    wpf_dll = os.path.join(path, "Microsoft.Web.WebView2.Wpf.dll")
    if os.path.isfile(wpf_dll):
        os.remove(wpf_dll)

    runtimes_path = os.path.join(path, "runtimes")
    if os.path.isdir(runtimes_path):
        shutil.rmtree(runtimes_path)


def windows_track(src_dir, build_dir, dist_output, release_output, project_root, no_pack, binary_name):
    try:
        safe_print("[Windows] Starting Parallel Track...")
        components = {
            "webview": (os.path.join(src_dir, "windows", "WebView", "WebView.csproj"), f"{binary_name}-webview"),
            "main": (os.path.join(src_dir, "windows", "Main", "Main.csproj"), binary_name),
            "server": (os.path.join(src_dir, "windows", "Server", "Server.csproj"), f"{binary_name}.Server")
        }

        temp_folders = {}
        with concurrent.futures.ThreadPoolExecutor(max_workers=3) as executor:
            future_to_name = {
                executor.submit(build_windows_component, name, path, build_dir, asm_name): name 
                for name, (path, asm_name) in components.items()
            }
            for future in concurrent.futures.as_completed(future_to_name):
                temp_folders[future_to_name[future]] = future.result()

        safe_print("[Windows] Organizing and cleaning distribution...")
        final_win_root = os.path.join(dist_output, "windows")
        if os.path.exists(final_win_root):
            shutil.rmtree(final_win_root)
        os.makedirs(final_win_root)

        for folder in temp_folders.values():
            for item in os.listdir(folder):
                src, dst = os.path.join(folder, item), os.path.join(final_win_root, item)
                if os.path.isdir(src):
                    if not os.path.exists(dst):
                        shutil.copytree(src, dst)
                else:
                    if not os.path.exists(dst):
                        shutil.copy2(src, dst)

        cleanup_windows_files(final_win_root)

        config_src = os.path.join(src_dir, "config.json")
        if os.path.exists(config_src):
            shutil.copy2(config_src, os.path.join(final_win_root, "config.json"))

        if not no_pack:
            safe_print("[Windows] Creating Zip archive...")
            shutil.make_archive(os.path.join(release_output, "windows"), "zip", final_win_root)
            safe_print("[Windows] Zip created.")
        safe_print("[Windows] Track Completed.")
    except Exception as e:
        safe_print(f"[Windows] Track failed: {e}")


def build_linux_track(src_dir, build_dir, dist_output, release_output, project_root, no_pack, binary_name):
    try:
        safe_print("[Linux] Starting Track...")
        linux_pkg_dir = os.path.join(build_dir, "linux_pkg")
        if os.path.exists(linux_pkg_dir):
            shutil.rmtree(linux_pkg_dir)
        os.makedirs(linux_pkg_dir)

        linux_src_dir = os.path.join(src_dir, "linux")
        for item in os.listdir(linux_src_dir):
            s, d = os.path.join(linux_src_dir, item), os.path.join(linux_pkg_dir, item)
            shutil.copy2(s, d) if not os.path.isdir(s) else shutil.copytree(s, d)

        # Rename launcher to binary_name
        run_sh = os.path.join(linux_pkg_dir, "run.sh")
        target_bin = os.path.join(linux_pkg_dir, binary_name)
        if os.path.exists(run_sh):
            os.rename(run_sh, target_bin)
            os.chmod(target_bin, 0o755)

        final_linux_root = os.path.join(dist_output, "linux")
        if os.path.exists(final_linux_root):
            shutil.rmtree(final_linux_root)
        shutil.copytree(linux_pkg_dir, final_linux_root)

        config_src = os.path.join(src_dir, "config.json")
        if os.path.exists(config_src):
            shutil.copy2(config_src, os.path.join(final_linux_root, "config.json"))

        if not no_pack:
            shutil.make_archive(os.path.join(release_output, "linux"), "zip", final_linux_root)
        safe_print("[Linux] Track Completed.")
    except Exception as e:
        safe_print(f"[Linux] Track failed: {e}")


def main():
    project_root = os.path.dirname(os.path.abspath(__file__))
    src_dir = os.path.join(project_root, "src")
    dist_output = os.path.join(project_root, "dist")
    release_output = os.path.join(project_root, "release")
    build_dir = os.path.join(project_root, "build")
    
    config = load_config(src_dir)
    binary_name = config.get("binary_name", "browser-as-wallpaper")
    
    no_pack = "--no-pack" in sys.argv
    target = None
    if "--target" in sys.argv:
        try:
            idx = sys.argv.index("--target")
            if idx + 1 < len(sys.argv):
                target = sys.argv[idx + 1]
        except ValueError:
            pass

    # Selective cleanup
    if target:
        for d in [os.path.join(dist_output, target), os.path.join(release_output, target + ".zip")]:
            if os.path.exists(d):
                if os.path.isdir(d): shutil.rmtree(d)
                else: os.remove(d)
    else:
        for d in [dist_output, release_output, build_dir]:
            if os.path.exists(d):
                shutil.rmtree(d)
            os.makedirs(d)
            
    if not os.path.exists(build_dir): os.makedirs(build_dir)
    if not os.path.exists(dist_output): os.makedirs(dist_output)
    if not os.path.exists(release_output): os.makedirs(release_output)

    safe_print("--- WaifuPaper Build System ---")
    total_start = time.time()

    with concurrent.futures.ThreadPoolExecutor() as executor:
        futures = []
        if target is None or target == "windows":
            futures.append(executor.submit(windows_track, src_dir, build_dir, dist_output, release_output, project_root, no_pack, binary_name))
        
        if target is None or target == "linux":
            futures.append(executor.submit(build_linux_track, src_dir, build_dir, dist_output, release_output, project_root, no_pack, binary_name))
        
        concurrent.futures.wait(futures)

    safe_print(f"\n[System] All builds finished in {time.time() - total_start:.2f}s")


if __name__ == "__main__":
    main()