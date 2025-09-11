import os
import time
import subprocess
import shutil

try:
    from dotenv import load_dotenv  # type: ignore
    load_dotenv()
except Exception as e:
    print("Failed to load .env. In case of problems you can hardcode here the .env values.")
    print(repr(e))
    raise SystemExit(1)
    
    


# --- Helper to read environment variables with defaults ---
def ENV(name: str, default: str) -> str:
    """Fetch an environment variable with a fallback default value."""
    return os.getenv(name, default)


# --- Base config  ---
input_folder = ENV("PIPE_INPUT_FOLDER", "")
new_input_name = ENV("PIPE_NEW_INPUT_NAME", "")
wsl_env = ENV("PIPE_WSL_ENV", "")
wsl_script = ENV("PIPE_WSL_SCRIPT", "")
output_folder_wsl = ENV("PIPE_OUTPUT_FOLDER_WSL", "")
trellis_assets_folder = ENV("PIPE_TRELLIS_ASSETS_FOLDER", "")
unity_assets_folder = ENV("PIPE_UNITY_ASSETS_FOLDER", "")
conda_profile = ENV("PIPE_CONDA_PROFILE", "")
# Derived config (depends on values defined via env variables in the main config section)
#script_dir = os.path.dirname(os.path.abspath(__file__))
ply_converter_script = os.path.join(PIPE_UNITY_ASSETS_FOLDER, "ply_converter.py")
converter_output_name = ENV("PIPE_CONVERTER_OUTPUT_NAME", "output.ply")

# How often (in seconds) the script checks for new files
poll_interval = int(ENV("PIPE_POLL_INTERVAL_SEC", "5"))


def wait_for_screenshot(folder):
    """Wait until a screenshot (.png or .jpg) appears in the specified folder."""
    while True:
        files = [f for f in os.listdir(folder) if f.endswith('.png') or f.endswith('.jpg')]
        if files:
            return os.path.join(folder, files[0])
        time.sleep(poll_interval)


def rename_file(old_path, new_name):
    """Rename a file and return the new path."""
    new_path = os.path.join(os.path.dirname(old_path), new_name)
    os.rename(old_path, new_path)
    return new_path


def prepare_screenshot(source_path, target_folder, new_name):
    """
    Move a screenshot into the target folder with a fixed name.
    If the target folder does not exist, it is created.
    If a file already exists, it will be overwritten.
    """
    target_folder = os.path.normpath(target_folder)
    os.makedirs(target_folder, exist_ok=True)

    target_path = os.path.join(target_folder, new_name)

    try:
        shutil.copy2(source_path, target_path)
    except shutil.SameFileError:
        pass

    os.remove(source_path)
    return target_path


def run_wsl_process(command):
    """Run a shell command inside WSL with login shell enabled."""
    full_command = f"wsl bash --login -c \"{command}\""
    subprocess.run(full_command, shell=True, check=True)


def wait_for_output_file(wsl_output_folder):
    """
    Poll a WSL directory until a .ply or .obj file appears.
    Returns the first valid file name found.
    """
    while True:
        result = subprocess.run(
            f"wsl bash -c \"ls {wsl_output_folder}\"",
            shell=True,
            capture_output=True,
            text=True
        )
        files = result.stdout.strip().split('\n')
        valid_files = [f for f in files if f.endswith(('.ply', '.obj'))]
        if valid_files:
            return valid_files[0]
        time.sleep(poll_interval)


def transfer_file_from_wsl(wsl_path, windows_dest_folder):
    """
    Move a file from WSL to a Windows folder.
    If a file with the same name exists, it is overwritten.
    """
    result = subprocess.run(
        f"wsl wslpath -w {wsl_path}",
        shell=True,
        capture_output=True,
        text=True
    )
    windows_src_path = result.stdout.strip()

    file_name = os.path.basename(windows_src_path)
    windows_dest_path = os.path.join(windows_dest_folder, file_name)

    os.makedirs(windows_dest_folder, exist_ok=True)

    if os.path.exists(windows_dest_path):
        os.chmod(windows_dest_path, 0o777)  # reset permissions
        os.remove(windows_dest_path)

    shutil.move(windows_src_path, windows_dest_path)
    print(f"✅ Moved: {windows_src_path} → {windows_dest_path}")


def convert_to_wsl_path(windows_path):
    """Convert a Windows path to a WSL-compatible path."""
    path = windows_path.replace("C:", "/mnt/c").replace("\\", "/")
    if " " in path:
        path = f'"{path}"'
    return path


def run_ply_converter(input_ply_path, output_folder):
    """
    Run the Unity converter script to transform a .ply file
    into a format with spherical harmonics coefficients.
    """
    print(f"🔄 Converting file {os.path.basename(input_ply_path)}...")

    converter_dir = os.path.dirname(ply_converter_script)

    if not os.path.exists(ply_converter_script):
        raise FileNotFoundError(f"Converter script not found: {ply_converter_script}")

    input_abs = os.path.abspath(input_ply_path)
    output_abs = os.path.abspath(os.path.join(output_folder, converter_output_name))

    cmd = [
        "python",
        f'"{os.path.basename(ply_converter_script)}"',
        f'--input "{input_abs}"',
        f'--output "{output_abs}"'
    ]

    try:
        subprocess.run(
            " ".join(cmd),
            shell=True,
            check=True,
            cwd=converter_dir
        )
        print(f"✅ Generated: {output_abs}")
    except subprocess.CalledProcessError as e:
        print(f"❌ Conversion error: {e}")
        print(f"Converter directory: {converter_dir}")
        print(f"Command: {' '.join(cmd)}")


def main():
    print("🟢 Waiting for screenshot from VR...")
    screenshot_path = wait_for_screenshot(input_folder)
    print(f"📷 Screenshot detected: {screenshot_path}")

    new_input_path = prepare_screenshot(
        source_path=screenshot_path,
        target_folder=trellis_assets_folder,
        new_name=new_input_name
    )
    print(f"✏️  Screenshot moved to: {new_input_path}")

    print("🚀 Running 3D reconstruction in WSL...")
    run_wsl_process(
        f"cd /mnt/c/models/TRELLIS && "
        f"source {conda_profile} && "
        f"conda activate {wsl_env} && "
        f"python {wsl_script}"
    )

    print("⏳ Waiting for 3D output file (Gaussian Splatting)...")
    output_file = wait_for_output_file(output_folder_wsl)
    print(f"📦 3D file generated: {output_file}")

    print("📤 Moving 3D model to Unity...")
    transfer_file_from_wsl(
        os.path.join(output_folder_wsl, output_file),
        unity_assets_folder
    )
    print("✅ Model moved to Unity assets folder.")

    input_ply_path = os.path.join(unity_assets_folder, output_file)

    run_ply_converter(input_ply_path, unity_assets_folder)

    final_output = os.path.join(unity_assets_folder, converter_output_name)
    if os.path.exists(final_output):
        print(f"✅ Final converted file ready: {final_output}")
    else:
        raise FileNotFoundError("Conversion failed: no output file produced.")

    trigger_path = os.path.join(unity_assets_folder, "reload.trigger")
    with open(trigger_path, 'w') as f:
        f.write("reload")


if __name__ == "__main__":
    main()
