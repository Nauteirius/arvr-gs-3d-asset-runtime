import numpy as np
import os
from plyfile import PlyData, PlyElement

unity_assets_folder = os.getenv("PIPE_UNITY_ASSETS_FOLDER", "")
#input_path = 'sample.ply'
#output_path = 'output.ply'
input_path = os.path.join(unity_assets_folder, "sample.ply")
output_path = os.path.join(unity_assets_folder, "output.ply")

# Read the original PLY file
with open(input_path, 'rb') as f:
    plydata = PlyData.read(f)

vertices = plydata['vertex'].data

# Ensure correct field order
original_fields = list(vertices.dtype.names)
split_index = original_fields.index('f_dc_2') + 1

# Create 45 new f_rest fields
new_rest_fields = [(f'f_rest_{i}', np.float32) for i in range(45)]

# Construct new dtype WITHOUT padding (crucial!)
new_dtype = np.dtype(
    vertices.dtype.descr[:split_index] + 
    new_rest_fields + 
    vertices.dtype.descr[split_index:],
    align=False  # Disable padding
)

# Verify correct size (should be 248 bytes)
assert new_dtype.itemsize == 248, f"Invalid vertex size: {new_dtype.itemsize} bytes"

# Create and populate new vertex data
new_vertices = np.zeros(vertices.shape, dtype=new_dtype)
for field in original_fields:
    new_vertices[field] = vertices[field]

# Save with correct binary format
new_vertex = PlyElement.describe(new_vertices, 'vertex')
PlyData([new_vertex], text=False).write(output_path)

print("Conversion successful! Verify with hex editor.")