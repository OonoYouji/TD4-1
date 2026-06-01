import json
import os
import glob
import shutil

def migrate_to_latest():
    # Paths (relative to the script location or project root)
    base_path = 'Project/Assets'
    scene_base_path = os.path.join(base_path, 'Scene')
    prefab_base_path = os.path.join(base_path, 'Prefabs')

    print("--- ONEngine Migration Tool: Upgrading to .scene/.entity format ---")

    # 1. Migrate Scene Files (.json -> .scene)
    json_scenes = glob.glob(os.path.join(scene_base_path, '*.json'))
    for scene_file in json_scenes:
        scene_name = os.path.splitext(os.path.basename(scene_file))[0]
        new_scene_file = os.path.join(scene_base_path, f"{scene_name}.scene")
        scene_dir = os.path.join(scene_base_path, scene_name)
        
        if not os.path.exists(scene_dir):
            os.makedirs(scene_dir)

        print(f"Converting scene: {scene_name}...")
        try:
            with open(scene_file, 'r', encoding='utf-8-sig') as f:
                scene_data = json.load(f)

            if 'entities' not in scene_data or (len(scene_data['entities']) > 0 and 'path' in scene_data['entities'][0]):
                print(f"  Skipping {scene_name} (already migrated or invalid format)")
                continue

            new_entities = []
            for entity in scene_data['entities']:
                if not isinstance(entity, dict): continue
                name = entity.get('name', 'Unknown')
                
                # Check for tuned variables in the subdirectory
                var_file = os.path.join(scene_dir, f"{name}.json")
                if os.path.exists(var_file):
                    with open(var_file, 'r', encoding='utf-8-sig') as vf:
                        vars = json.load(vf)
                        for comp in entity.get('components', []):
                            if comp.get('type') == 'Variables' and isinstance(vars, dict):
                                comp.update(vars)
                    os.remove(var_file)

                # Save .entity
                entity_file_name = f"{name}.entity"
                with open(os.path.join(scene_dir, entity_file_name), 'w', encoding='utf-8') as ef:
                    json.dump(entity, ef, indent=4)

                new_entities.append({
                    "path": f"./{scene_name}/{entity_file_name}",
                    "id": entity.get('id', 0),
                    "parent": entity.get('parent')
                })

            with open(new_scene_file, 'w', encoding='utf-8') as sf:
                json.dump({"entities": new_entities}, sf, indent=4)
            
            os.remove(scene_file)
            print(f"  Successfully migrated {scene_name}.scene")

        except Exception as e:
            print(f"  Error migrating scene {scene_name}: {e}")

    # 2. Migrate Prefabs (Merge standalone .json variables)
    prefab_files = glob.glob(os.path.join(prefab_base_path, '*.prefab'))
    for prefab_file in prefab_files:
        prefab_name = os.path.splitext(os.path.basename(prefab_file))[0]
        json_var_file = os.path.join(prefab_base_path, f"{prefab_name}.json")
        
        if os.path.exists(json_var_file):
            print(f"Merging variables into prefab: {prefab_name}...")
            try:
                with open(prefab_file, 'r', encoding='utf-8-sig') as pf:
                    prefab_data = json.load(pf)
                with open(json_var_file, 'r', encoding='utf-8-sig') as jf:
                    var_data = json.load(jf)

                found = False
                for comp in prefab_data.get('components', []):
                    if comp.get('type') == 'Variables' and isinstance(var_data, dict):
                        comp.update(var_data)
                        found = True
                
                if not found and isinstance(var_data, dict):
                    if 'components' not in prefab_data: prefab_data['components'] = []
                    v_comp = {"type": "Variables"}
                    v_comp.update(var_data)
                    prefab_data['components'].append(v_comp)

                with open(prefab_file, 'w', encoding='utf-8') as pf:
                    json.dump(prefab_data, pf, indent=4)
                
                os.remove(json_var_file)
                print(f"  Successfully upgraded {prefab_name}.prefab")
            except Exception as e:
                print(f"  Error upgrading prefab {prefab_name}: {e}")

    print("--- Migration Finished ---")

if __name__ == "__main__":
    migrate_to_latest()
