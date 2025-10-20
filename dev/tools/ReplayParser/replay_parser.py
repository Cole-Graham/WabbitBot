import json
import re
import os

def parse_game_replay(file_path, output_path):
    with open(file_path, 'rb') as file:  # Open in binary mode
        # Read the entire file to capture both game metadata and result data
        file_data = file.read()
        
        # Decode using UTF-8 with error handling
        combined_line = file_data.decode('utf-8', errors='ignore')
        
        # Find the game metadata JSON
        json_start = combined_line.find('{"game":')  # Search for the string
        if json_start == -1:
            print("No valid JSON data found.")
            return
        
        # Extract the game metadata JSON (stop at "star" or before result)
        json_data = combined_line[json_start:]
        star_index = json_data.find('star')
        if star_index != -1:
            game_json = json_data[:star_index]
        else:
            game_json = json_data
        
        # Clean and parse the game JSON
        game_json = _clean_json_string(game_json)
        
        try:
            data = json.loads(game_json)
        except json.JSONDecodeError as e:
            print(f"Error decoding game JSON: {e}")
            print(f"Extracted JSON data: {game_json}")  # Print the problematic JSON for debugging
            return
        
        # Look for the result JSON (separate object with Duration and Victory)
        result_match = re.search(r'\{"Duration":"(\d+)","Victory":"(\d+)"\}', combined_line)
        if result_match:
            data['result'] = {
                'Duration': result_match.group(1),
                'Victory': result_match.group(2),
            }
            print(f"Found result: Duration={result_match.group(1)}s, Victory={result_match.group(2)}")
        else:
            print("Warning: No result data found in replay")
        
        # Format the data
        formatted_data = _format_data(data)

        # Ensure the output directory exists
        output_dir = os.path.dirname(output_path)
        if output_dir and not os.path.exists(output_dir):
            os.makedirs(output_dir)

        # Write the ASCII-safe version (with Unicode escapes)
        with open(output_path, 'w', encoding='utf-8') as output_file:
            json.dump(formatted_data, output_file, indent=4)
        
        print(f"ASCII-safe formatted data written to {output_path}")

        # Write the Unicode version (with actual characters)
        base_name, ext = os.path.splitext(output_path)
        unicode_output_path = f"{base_name}.unicode{ext}"
        with open(unicode_output_path, 'w', encoding='utf-8') as output_file:
            json.dump(formatted_data, output_file, indent=4, ensure_ascii=False)
        
        print(f"Unicode formatted data written to {unicode_output_path}")

def _clean_json_string(json_string):
    # Remove excessive backslashes
    json_string = re.sub(r'\\+', r'\\', json_string)  # Normalize backslashes
    json_string = json_string.replace('\\"', '"')  # Replace escaped quotes with normal quotes
    
    # Remove any leading or trailing whitespace
    json_string = json_string.strip()
    
    return json_string

def _format_data(data):
    # Reorganize the data as needed
    formatted = {
        "game": {
            "GameMode": data.get("game", {}).get("GameMode"),
            "AllowObservers": data.get("game", {}).get("AllowObservers"),
            "ObserverDelay": data.get("game", {}).get("ObserverDelay"),
            "Seed": data.get("game", {}).get("Seed"),
            "Private": data.get("game", {}).get("Private"),
            "ServerName": data.get("game", {}).get("ServerName"),
            "Version": data.get("game", {}).get("Version"),
            "UniqueSessionId": data.get("game", {}).get("UniqueSessionId"),
            "ModList": data.get("game", {}).get("ModList"),
            "ModTagList": data.get("game", {}).get("ModTagList"),
            "EnvironmentSettings": data.get("game", {}).get("EnvironmentSettings"),
            "GameType": data.get("game", {}).get("GameType"),
            "Map": data.get("game", {}).get("Map"),
            "InitMoney": data.get("game", {}).get("InitMoney"),
            "TimeLimit": data.get("game", {}).get("TimeLimit"),
            "ScoreLimit": data.get("game", {}).get("ScoreLimit"),
            "CombatRule": data.get("game", {}).get("CombatRule"),
            "IncomeRate": data.get("game", {}).get("IncomeRate"),
            "Upkeep": data.get("game", {}).get("Upkeep"),
            "Players": []
        },
        "result": data.get("result")  # Include result data if available
    }
    
    # Extract player data
    for key in data.keys():
        if key.startswith("player_"):
            player_info = data[key]
            formatted["game"]["Players"].append({
                "PlayerUserId": player_info.get("PlayerUserId"),
                "PlayerName": player_info.get("PlayerName"),
                "PlayerElo": player_info.get("PlayerElo"),
                "PlayerLevel": player_info.get("PlayerLevel"),
                "PlayerAlliance": player_info.get("PlayerAlliance"),
                "PlayerScoreLimit": player_info.get("PlayerScoreLimit"),
                "PlayerIncomeRate": player_info.get("PlayerIncomeRate"),
                "PlayerAvatar": player_info.get("PlayerAvatar"),
                "PlayerReady": player_info.get("PlayerReady"),
                "PlayerDeckContent": player_info.get("PlayerDeckContent"),
                "PlayerDeckName": player_info.get("PlayerDeckName"),
            })
    
    return formatted

# Example usage
if __name__ == "__main__":
    input_file_path = 'C:/Users/coleg/Saved Games/EugenSystems/WARNO/replay_2025-10-17_14-35-11.rpl3'  # Change this to your input file path

    # Generate output path based on input filename
    input_filename = os.path.basename(input_file_path)
    output_filename = os.path.splitext(input_filename)[0] + '.json'
    output_file_path = os.path.join('out', output_filename)

    parse_game_replay(input_file_path, output_file_path)
