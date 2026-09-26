#----------------------------------------------------------------
#  Discord_RPC.rb
#
#  Changelog:
#      Paulinchen  2026-09-26: Kept the per-save counters inside the save, taking over the earlier versions' files once
#                            - Added an option to count the trivia across all saves instead of per save
#                            - Added an NSFW option to the game's Config menu, or to the Mod Config Menu when it is installed
#                            - Counted the requests per save, in total and per character, and published a running one
#                            - Counted the defeat scenes per save, in total and per monster girl, and published a running one
#                            - Published the time of the last button press, so Discord can show when the player is idle
#                            - Added the music that is playing to the trivia
#                            - Moved into the Patch folder, where the community's mod loader picks it up
#                            - Deleted the earlier versions' script, and skipped hooks in place or still loaded by their block
#      Paulinchen  2026-09-25: Created
#
#----------------------------------------------------------------

# Hands the game state to Discord/DiscordPresence.dll, which talks to Discord on a thread of its
# own. It must never interrupt the game, so every entry point rescues.
module MGQ_Discord
  # Turns the mod off without uninstalling it.
  ENABLED = true

  # Logs every published status to InGame.log.
  DEBUG = false

  # Folder next to Game.exe that holds everything the mod reads or writes.
  MOD_DIR = "Discord"

  # Frames between two publishes, half a second at 60 frames per second.
  PUBLISH_INTERVAL = 30

  # Start of this game session in Unix seconds, shown on Discord as elapsed time.
  STARTED_AT = Time.now.to_i

  # Game script of the earlier versions, which a block they added to Patch.rb loads after the mod
  # loader. Relative to the working directory, like in that block.
  LEGACY_SCRIPT = "Discord/rpc.rb"

  # The file the game and the earlier versions' block are loaded from.
  PATCH_FILE = "Patch/Patch.rb"

  # First line of the block the earlier versions added to PATCH_FILE.
  LEGACY_BLOCK_MARKER = "# >>> MGQ Discord RPC"

  # Reports whether the game hooks can be installed, deleting the game script of the earlier
  # versions on the way.
  #
  # Another copy of this script or an earlier version wraps the same methods under the same names,
  # and each hook would then call itself until the stack overflows.
  #
  # @return [Boolean] false when the hooks are in place already, or the earlier version would add them
  def self.hookable?
    retired = retire_legacy_script

    if Graphics.respond_to?(:mgq_discord_update)
      Log.write("hooks skipped: another copy or an earlier version installed them already")
      return false
    end

    return true if retired || !legacy_block_present?

    Log.write("hooks skipped: the earlier version's block in #{PATCH_FILE} would still load #{LEGACY_SCRIPT}")
    false
  end

  # Deletes the game script of the earlier versions, so their block in Patch.rb finds nothing to load.
  #
  # @return [Boolean] whether it is gone
  def self.retire_legacy_script
    return true unless File.exist?(LEGACY_SCRIPT)

    File.delete(LEGACY_SCRIPT)
    Log.write("deleted #{LEGACY_SCRIPT} of an earlier version")
    true
  rescue => e
    Log.write("could not delete #{LEGACY_SCRIPT} of an earlier version: #{e.class}: #{e.message}")
    false
  end

  # Reports whether Patch.rb still holds the block of the earlier versions, which loads LEGACY_SCRIPT.
  #
  # @return [Boolean] whether the block is present, true when Patch.rb cannot be read
  def self.legacy_block_present?
    File.open(PATCH_FILE, "rb") { |file| file.read }.include?(LEGACY_BLOCK_MARKER)
  rescue
    true
  end

  # Starts the presence, once per game session.
  def self.start
    return if @started
    @started = true
    return unless ENABLED && Presence.installed?

    @running = Presence.start == 1
    publish("title") if @running
  rescue => e
    Log.write("boot failed: #{e.class}: #{e.message}")
  end

  # Notes button presses and publishes the status every PUBLISH_INTERVAL frames. Called once per frame.
  def self.tick
    return unless @running

    @last_input_at = Time.now.to_i if GameState.input?
    @frames = (@frames || 0) + 1
    return if @frames < PUBLISH_INTERVAL

    @frames = 0
    Options.sync
    publish(GameState.scene)
  rescue => e
    @frames = 0
    Log.write("tick failed: #{e.class}: #{e.message}")
  end

  # Publishes the status, unless it is unchanged since the last time.
  #
  # @param scene [String] what the game is showing, see GameState.scene
  def self.publish(scene)
    status = StatusText.build(scene)
    return if status == @published

    @published = status
    Log.write("publishing: #{status.lines.first(4).join(' ').gsub("\n", '')}") if DEBUG
    Presence.update(status)
  rescue => e
    Log.write("write failed: #{e.class}: #{e.message}")
  end

  # When the player last pressed a button, in Unix seconds.
  #
  # @return [Integer] the time, the start of the game session until the first press
  def self.last_input_at
    @last_input_at || STARTED_AT
  end

  # Builds the path of a file inside the mod folder.
  #
  # @param name [String] the file name, relative to the mod folder
  # @return [String] the full path
  def self.path(name)
    "#{game_dir}/#{MOD_DIR}/#{name}"
  end

  # The folder of Game.exe.
  #
  # Asked of Windows, the working directory is wherever a shortcut or Steam started the game.
  #
  # @return [String] the folder, with forward slashes
  def self.game_dir
    @game_dir ||= begin
      buffer = "\0" * 512
      length = Win32API.new('kernel32', 'GetModuleFileNameA', 'lpl', 'l').call(0, buffer, 512)
      File.dirname(buffer[0, length].tr("\\", "/"))
    end
  rescue
    @game_dir = Dir.pwd
  end

  # Reads a value from the game as text.
  #
  # At the title screen $game_map exists but holds no map, so even display_name raises.
  #
  # @yieldreturn [Object] the value
  # @return [String] the value as text, or "" when it is nil or reading it failed
  def self.text_of
    value = yield
    value.nil? ? "" : value.to_s
  rescue
    ""
  end

  # Discord/InGame.log, which only appears when something went wrong inside the game.
  module Log
    # Lines written per session at most, an error repeating every frame would flood the file.
    MAX_LINES = 60

    @lines = 0

    # Appends a line, prefixed with the time.
    #
    # @param message [String] the line to append
    def self.write(message)
      return if @lines >= MAX_LINES
      @lines += 1

      File.open(MGQ_Discord.path("InGame.log"), "ab") { |file| file.write("#{Time.now}  #{message}\n") }
    rescue
    end
  end

  # The mod's options, in the Mod Config Menu when it is installed, in the game's Config menu otherwise.
  #
  # The game keeps its options inside each save. The mod's options are the same for every save, so
  # they live in Discord/Settings.ini, are handed to whichever save is loaded and left out of the
  # save files.
  module Options
    # File inside the mod folder, shared with DiscordPresence.dll and edited by players too.
    FILE = "Settings.ini"

    # Whether requests and defeat scenes show on Discord, 0 or 1.
    NSFW = :mod_discord_nsfw

    # Whether the trivia counts across all saves (1) or per save (0).
    ALL_SAVES = :mod_discord_all_saves

    # Every option by its key in $game_system.conf, with its key in FILE.
    NAMES = { NSFW => "nsfw", ALL_SAVES => "all_saves" }

    # Every option, by its key in $game_system.conf.
    KEYS = NAMES.keys

    # How each option shows in the menu: its name, its help, and a name and help per value. The
    # first value is the default.
    MENU = {
      NSFW => {
        :name   => "[Discord] NSFW",
        :help   => "Show requests and defeat scenes, and how often they happened, on Discord.",
        :values => {
          0 => ["Off", "Discord never mentions requests or defeat scenes."],
          1 => ["On",  "Discord shows a running request or defeat scene, and their counters."],
        },
      },
      ALL_SAVES => {
        :name   => "[Discord] Statistics",
        :help   => "Count the trivia per save or across all saves.",
        :values => {
          1 => ["All saves", "The game's own counts across every save."],
          0 => ["This save", "Counted by the mod for the loaded save, since the mod was installed."],
        },
      },
    }

    # Adds the options to the menu.
    #
    # The Mod Config Menu defines MOD_CONTENTS in 0_ModConfigMenu.rb, which the mod loader runs
    # before this script.
    def self.register
      config = NWConst::Config
      menu = config.const_defined?(:MOD_CONTENTS) ? config::MOD_CONTENTS : config::CONTENTS

      MENU.each do |key, option|
        menu.insert(-2, { :key => key, :name => option[:name], :sub => true, :help => "#{option[:help]}\r\n←/→ Toggle" })
        config::DATA[key] = option[:values].keys
        config::DATA_TEXT[key] = {}
        option[:values].each { |value, (name, help)| config::DATA_TEXT[key][value] = { :name => name, :help => help } }
        config::DEFAULT[key] = option[:values].keys.first
      end
    end

    # @return [Boolean] whether requests and defeat scenes show on Discord
    def self.nsfw?
      self[NSFW] == 1
    end

    # @return [Boolean] whether the trivia counts across all saves instead of per save
    def self.all_saves?
      self[ALL_SAVES] == 1
    end

    # @param key [Symbol] the option
    # @return [Integer] its value, the menu's default until it was changed
    def self.[](key)
      values.fetch(key) { NWConst::Config::DEFAULT[key] }
    end

    # Keeps the options of the loaded save and FILE in step. Called before every publish.
    #
    # Loading a save, starting a new game or returning to the title brings a new $game_system.conf,
    # which gets the stored options. Any other difference was made in the menu and gets stored.
    def self.sync
      return unless $game_system
      conf = $game_system.conf

      unless conf.equal?(@conf)
        @conf = conf
        KEYS.each { |key| conf[key] = self[key] }
        return
      end

      changed = KEYS.reject { |key| conf[key].nil? || conf[key] == self[key] }
      return if changed.empty?

      changed.each { |key| values[key] = conf[key] }
      write(changed)
    rescue => e
      Log.write("options sync failed: #{e.class}: #{e.message}")
    end

    # Writes options to FILE, replacing their lines or appending them. Every other line stays as it is.
    #
    # @param keys [Array<Symbol>] the options to write
    def self.write(keys)
      lines = File.open(MGQ_Discord.path(FILE), "rb") { |file| file.read }.each_line.to_a rescue []
      newline = lines.first.to_s.end_with?("\r\n") ? "\r\n" : "\n"
      lines[-1] += newline unless lines.empty? || lines[-1].end_with?("\n")

      keys.each do |key|
        entry = "#{NAMES[key]} = #{values[key]}#{newline}"
        index = lines.index { |line| name_of(line) == NAMES[key] }
        if index
          lines[index] = entry
        else
          lines << entry
        end
      end

      File.open(MGQ_Discord.path(FILE), "wb") { |file| file.write(lines.join) }
    end

    # Runs a block with the options taken out of $game_system.conf, so a save written inside it is
    # the same as one written without the mod.
    #
    # The block runs even when taking them out fails, since it writes the player's save.
    def self.left_out_of_save
      conf = $game_system.conf rescue nil
      kept = KEYS.select { |key| conf.key?(key) }.map { |key| [key, conf.delete(key)] } rescue []
      yield
    ensure
      kept.each { |key, value| conf[key] = value } if kept
    end

    # The stored options, read from FILE once.
    #
    # @return [Hash{Symbol => Integer}] the values by option
    def self.values
      @values ||= begin
        File.open(MGQ_Discord.path(FILE), "rb") { |file| file.read }.each_line.each_with_object({}) do |line, stored|
          key = NAMES.key(name_of(line))
          stored[key] = line.split("=", 2)[1].to_i if key
        end
      rescue
        {}
      end
    end

    # Reads the key of a line in FILE, compared without regard to case like DiscordPresence.dll does.
    #
    # @param line [String] the line
    # @return [String, nil] the key in lower case, nil for a comment, a section header or a line without one
    def self.name_of(line)
      line = line.strip
      return nil if line.start_with?("#", ";", "[")

      name, value = line.split("=", 2)
      value && name.strip.downcase
    end
  end

  # Numbers the way the presence writes them.
  module NumberFormat
    # Groups the thousands with commas: 1234567 becomes "1,234,567".
    #
    # @param number [Integer] the number
    # @return [String] the grouped number
    def self.grouped(number)
      number.to_i.to_s.reverse.scan(/\d{1,3}/).join(",").reverse
    end

    # Writes a count together with its noun: "1 battle", "2,345 battles".
    #
    # @param number [Integer] the count
    # @param singular [String] the noun for exactly one
    # @param plural [String] the noun for any other count
    # @return [String] the count with its noun
    def self.counted(number, singular, plural = singular + "s")
      "#{grouped(number)} #{number.to_i == 1 ? singular : plural}"
    end

    # Shortens a large number the way the game does: 28879000000000000 becomes "28.879Qdr.".
    #
    # give_unit comes from the game's LargeNumberConversion plugin.
    #
    # @param number [Integer] the number
    # @return [String] the shortened number
    def self.large(number)
      number = number.to_i
      number >= 10**6 && number.respond_to?(:give_unit) ? number.give_unit : grouped(number)
    end
  end

  # What the game currently shows and holds.
  module GameState
    # Progress inside the Labyrinth of Chaos.
    Labyrinth = Struct.new(:floor, :kind, :rare_points)

    # Every button of the game's Input module, keyboard and gamepad alike.
    BUTTONS = [:DOWN, :LEFT, :RIGHT, :UP, :A, :B, :C, :X, :Y, :Z, :L, :R, :SHIFT, :CTRL, :ALT]

    # Jobs and races an actor has taken to their maximum level.
    class Mastery < Struct.new(:jobs, :races)
      # @return [Integer] jobs and races together
      def total
        jobs + races
      end
    end

    # Reports whether the player holds any button down.
    #
    # The game has no mouse support, so the mouse never counts.
    #
    # @return [Boolean]
    def self.input?
      BUTTONS.any? { |button| Input.press?(button) }
    end

    # Reports whether a save is loaded.
    #
    # Before one is, the game holds placeholders: an empty party, no gold, map 0.
    #
    # @return [Boolean]
    def self.save_loaded?
      $game_map.map_id > 0
    rescue
      false
    end

    # Reads a switch by its name in the editor, so no id is hard-coded.
    #
    # @param name [String] the name of the switch
    # @return [Boolean, nil] the switch, nil when no switch has that name
    def self.switch_on?(name)
      id = $data_system.switches.index(name)
      id && $game_switches[id]
    end

    # Reads a variable by its name in the editor, so no id is hard-coded.
    #
    # @param name [String] the name of the variable
    # @return [Integer] the value, 0 when no variable has that name
    def self.variable(name)
      id = $data_system.variables.index(name)
      id ? $game_variables[id].to_i : 0
    end

    # Reports whether the party is inside the Labyrinth of Chaos.
    #
    # Its floors are named after their biome, so only the game's own flag tells.
    #
    # @return [Boolean]
    def self.in_labyrinth?
      switch_on?("Within Chaos Labyrinth") || variable("Within Chaos Labyrinth") > 0
    rescue
      false
    end

    # Reports whether the party is on the world map.
    #
    # The labyrinth's outdoor floors use the world map's field tilesets too.
    #
    # @return [Boolean]
    def self.on_world_map?
      $game_map.overworld? && !in_labyrinth?
    rescue
      false
    end

    # Tells what the game is showing.
    #
    # The world map's name is untranslated kanji, so a menu opened there stays "travel" rather
    # than naming the map.
    #
    # @return [String] "title", "battle", "request", "defeat_scene", "travel", "map" or "menu"
    def self.scene
      scene = SceneManager.scene
      return "title" if scene.nil?

      name = scene.class.name.to_s
      return "battle"  if name =~ /Battle/
      return "title"   if name =~ /Title/
      return "request" if Options.nsfw? && Requests.current
      return "defeat_scene" if Options.nsfw? && DefeatScenes.current
      return "travel"  if on_world_map?
      return "map"    if name =~ /Map/
      "menu"
    rescue
      "map"
    end

    # Tells how the party crosses the world map.
    #
    # @return [String] "air", "sea" or "foot"
    def self.vehicle
      player = $game_player
      return "air" if player.in_airship?
      return "sea" if player.in_boat? || player.in_ship?
      "foot"
    rescue
      "foot"
    end

    # Names the current map.
    #
    # Many maps leave their display name blank, which falls back to the name in the editor.
    #
    # @return [String] the name, or "" when there is none
    def self.area
      name = MGQ_Discord.text_of { $game_map.display_name }
      name = MGQ_Discord.text_of { $data_mapinfos[$game_map.map_id].name } if name.empty?
      name
    end

    # Reads the progress inside the Labyrinth of Chaos.
    #
    # A Carnage floor counter above 0 is taken to mean Carnage, which is untested.
    # "Labyrinth of Chaos: Type" does not tell the two apart.
    #
    # @return [Labyrinth, nil] the progress, nil outside the labyrinth
    def self.labyrinth
      return nil unless save_loaded? && in_labyrinth?

      carnage_floor = variable("Carnage Labyrinth of Chaos Current Floor")
      carnage = carnage_floor > 0

      Labyrinth.new(carnage ? carnage_floor : variable("Chaos Labyrinth Current LV"),
                    carnage ? "Carnage" : "Normal",
                    variable("Labyrinth of Chaos Rare Value"))
    end

    # Counts the jobs and races an actor has mastered.
    #
    # A job or race whose level in level_list reached its max_lv counts as mastered.
    #
    # @param actor [Game_Actor] the actor
    # @return [Mastery] the mastered jobs and races
    def self.mastery(actor)
      mastery = Mastery.new(0, 0)

      actor.level_list.each do |class_id, level|
        data = $data_classes[class_id]
        next if data.nil? || level < data.max_lv

        if NWConst::Class::JOB_RANGE.include?(class_id)
          mastery.jobs += 1
        elsif NWConst::Class::TRIBE_RANGE.include?(class_id)
          mastery.races += 1
        end
      end

      mastery
    end
  end

  # Requests: scenes a recruited monster plays when the player asks her to, in the Pocket Castle
  # or aboard the MS Fish. They are counted with or without the NSFW option, only showing them
  # depends on it.
  module Requests
    # How the Recollection Room names a request: "Request 1 (...)", "Plea 2 (...)" and so on.
    # Its defeat, battle and story scenes are named otherwise.
    SCENE_NAME = /Request|Plea|Beg|Enticement|おねだり/

    # Counts a novel scene that just started, if it is a request. Called from the Game_Novel#setup hook.
    #
    # @param event_id [Integer] the common event the scene plays
    def self.started(event_id)
      character = character_of(event_id)
      SaveStats.add_for(:requests, character) if character
    end

    # @return [String, nil] who plays the running request, nil while none runs
    def self.current
      $game_novel && $game_novel.running? ? character_of($game_novel.event_id) : nil
    end

    # Names who plays a request.
    #
    # The Recollection Room replays requests through the same novel scenes, with the game's
    # LIBRARY_H_MEMORY switch on.
    #
    # @param event_id [Integer] the common event a novel scene plays
    # @return [String, nil] the character, nil for any other scene and for a replay
    def self.character_of(event_id)
      return nil if $game_switches[NWConst::Sw::LIBRARY_H_MEMORY]

      characters[event_id]
    end

    # Characters by the common events of their requests, read from the Recollection Room once.
    #
    # A request is named after the companion who plays it, falling back to the Recollection Room's
    # entry without the form in brackets: "Alice (Small)" and "Alice (Adult)" both count as Alice.
    #
    # @return [Hash{Integer => String}] the characters by common event
    def self.characters
      @characters ||= NWConst::Library::H_SCENE_ITEMS.values.each_with_object({}) do |character, names|
        entry = character[:name].to_s.sub(/\s*[(（].*\z/m, "")

        (character[:items] || {}).each_value do |item|
          names[item[:common]] ||= companion_of(item) || entry if item[:name].to_s =~ SCENE_NAME
        end
      end
    end

    # Names the companion a request belongs to.
    #
    # Most requests unlock at a companion's affection, which the game keeps in variable
    # ACTOR_REL_BASE + her actor id. The name comes from the database, since looking her up in
    # $game_actors would add her to the save.
    #
    # @param item [Hash] the request's entry in the Recollection Room
    # @return [String, nil] the companion's name, nil when the request unlocks otherwise
    def self.companion_of(item)
      condition = item[:condition] || {}
      return nil unless condition[:type] == 1

      actor_id = condition[:id].to_i - NWConst::Var::ACTOR_REL_BASE
      actor = actor_id > 0 && $data_actors[actor_id]
      actor && !actor.name.to_s.empty? ? actor.name.to_s : nil
    end
  end

  # Defeat scenes: what the monster girl who won a battle does to Luka. Counted with or without
  # the NSFW option, only showing them depends on it.
  module DefeatScenes
    # Counts the defeat scene BattleManager just set up, unless it is a replay or was skipped.
    # Called from the BattleManager.change_novel_scene hook.
    #
    # The Labyrinth of Chaos plays LOSE_EVENT_BASE itself for all its monsters, which has no scene
    # of her own.
    def self.started
      return if BattleManager.memory_battle? || $game_switches[NWConst::Sw::LIBRARY_H_MEMORY]

      event_id = $game_temp.lose_event_id
      enemy = $data_enemies[$game_temp.lose_event_enemy_id]
      return if event_id <= NWConst::Common::LOSE_EVENT_BASE || enemy.nil? || skipped?(event_id)

      @running = [event_id, enemy.name.to_s]
      SaveStats.add_for(:rapes, enemy.name.to_s)
    end

    # Forgets the last defeat scene, so another novel scene with the same event is not taken for
    # it. Called from the Game_Novel#setup hook.
    def self.forget
      @running = nil
    end

    # @return [String, nil] the monster girl of the running defeat scene, nil while none runs
    def self.current
      event_id, monster = @running
      monster if event_id && $game_novel && $game_novel.running? && $game_novel.event_id == event_id
    end

    # Reports whether the player chose to skip the scene.
    #
    # Skipping replaces the novel's event list with a copy that starts after the scene.
    #
    # @param event_id [Integer] the common event of the defeat scene
    # @return [Boolean]
    def self.skipped?(event_id)
      !$game_novel.interpreter.instance_variable_get(:@list).equal?($data_common_events[event_id].list)
    end
  end

  # The second Discord line. The presence shows one of these at a time.
  module Trivia
    # Seconds the lines are kept before they are worked out again.
    RECOMPUTE_SECONDS = 5

    # Seconds a used item stays among the lines.
    ITEM_USE_SECONDS = 90

    # Seconds before the top stat and top master lines move on to the next one.
    ROTATION_SECONDS = 45

    # Number of base parameters an actor has.
    PARAM_COUNT = 8

    # Name and comment by the value of the game's difficulty variable.
    DIFFICULTIES = {
      -2 => ["Very Easy", "Here for the story, and that's fine"],
      -1 => ["Easy",      "Taking it nice and slow"],
      0  => ["Normal",    "The way it's meant to be played"],
      1  => ["Hard",      "Starting to sweat a little"],
      2  => ["Very Hard", "Pain is a choice, and they chose it"],
      3  => ["Hell",      "Welcome to hell, enjoy your stay"],
      4  => ["Paradox",   "Has lost all sense of self-preservation"],
    }

    # Comment on the playtime by the hours it takes, the highest one reached is shown.
    PLAYTIME_COMMENTS = [
      [0,    "Still an Apprentice Hero fresh out of Iliasville"],
      [10,   "Has learned that every loss is a new bad end"],
      [25,   "Starting to understand the job system... probably"],
      [50,   "The Pocket Castle is starting to feel like home"],
      [100,  "The job and race grind has begun in earnest"],
      [200,  "Has seen more of the Labyrinth of Chaos than the sun"],
      [400,  "Ilias has stopped answering their prayers"],
      [700,  "The grind never ends, and neither do they"],
      [1000, "Has become the true Paradox"],
    ]

    # The last item an actor used, on whom and when.
    ItemUse = Struct.new(:item, :target, :used_at)

    # Every line, in the order they rotate. Each returns its text, or nil to be left out for now.
    LINES = [
      :dead_party_members,
      :recruited_members,
      :battles_fought,
      :difficulty,
      :playtime,
      :chosen_side,
      :last_item_used,
      :current_track,
      :top_master,
      :enemies_defeated,
      :battles_escaped,
      :wipeouts,
      :top_stat,
      :biggest_hit,
      :gold_spent,
      :items_synthesized,
      :deepest_labyrinth_floor,
      :requests_made,
      :most_requested,
      :times_raped,
      :most_raped_by,
      :gold_carried,
    ]

    # The lines that currently apply.
    #
    # Worked out at most every RECOMPUTE_SECONDS, and at once when another save is loaded.
    #
    # @return [Array<String>] the lines, none before a save is loaded
    def self.lines
      return [] unless GameState.save_loaded?

      now = Time.now.to_i
      game = $game_system.object_id
      return @lines if @lines && @game == game && now - @computed_at < RECOMPUTE_SECONDS

      @computed_at = now
      @game = game
      @lines = LINES.map { |line| MGQ_Discord.text_of { send(line) } }.reject { |text| text.empty? }
    end

    # Remembers an item an actor just used. Called from the Game_Battler#item_apply hook.
    #
    # @param item [RPG::Item] the item
    # @param target [Game_Battler] the one it was used on
    def self.item_used(item, target)
      @item_use = ItemUse.new(item.name.to_s, target.name.to_s, Time.now.to_i)
    end

    # Counts up every ROTATION_SECONDS, which picks the top stat and the top master.
    #
    # @return [Integer] the current rotation
    def self.rotation
      Time.now.to_i / ROTATION_SECONDS
    end

    # How many of the active party are down.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.dead_party_members
      count = $game_party.battle_members.count { |actor| actor.dead? }
      "Currently has #{NumberFormat.counted(count, 'dead party member')}!" if count > 0
    end

    # How many companions have joined, Luka not counted.
    #
    # Read from the permanent roster, since include_members returns only the temporary party
    # during story sections like the Chaos domain.
    #
    # @return [String] the line
    def self.recruited_members
      roster = $game_party.instance_variable_get(:@include_actors).map { |id| $game_actors[id] }
      count = roster.reject { |actor| actor.luca? }.size
      "Has recruited #{count} party members this playthrough!"
    end

    # How many battles this save has fought.
    #
    # @return [String] the line
    def self.battles_fought
      "Has fought #{NumberFormat.grouped($game_system.battle_count)} battles!"
    end

    # The difficulty, with a comment on it.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.difficulty
      name, comment = DIFFICULTIES[$game_variables[NWConst::Var::CURRENT_DIFFICULTY]]
      "Currently playing on #{name} - #{comment}!" if name
    end

    # The playtime in hours, with a comment on it.
    #
    # @return [String] the line
    def self.playtime
      hours = $game_system.playtime / 3600
      comment = PLAYTIME_COMMENTS.select { |minimum, _| hours >= minimum }.last[1]

      if hours == 0
        "Is less than an hour in. #{comment}!"
      else
        "Is #{NumberFormat.counted(hours, 'hour')} in. #{comment}!"
      end
    end

    # Whether Ilias or Alice was chosen.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.chosen_side
      if GameState.switch_on?("Ilias Chosen")
        "Has chosen Ilias this playthrough!"
      elsif GameState.switch_on?("Alice Chosen")
        "Has chosen Alice this playthrough!"
      end
    end

    # The item an actor used last, for ITEM_USE_SECONDS.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.last_item_used
      use = @item_use
      "Just used #{use.item} on #{use.target}!" if use && Time.now.to_i - use.used_at <= ITEM_USE_SECONDS
    end

    # The music that is playing, named like in the music room of Kagetsumugi's jukebox.
    #
    # RPG::BGM.last is the game's own record of the music started last, emptied when it stops.
    #
    # @return [String, nil] the line, nil when no music plays or the jukebox does not know the track
    def self.current_track
      title = track_titles[File.basename(RPG::BGM.last.name.to_s, ".*").downcase]
      "Currently vibing to #{title}!" if title
    end

    # Track names by BGM file name, read from the music room once.
    #
    # @return [Hash{String => String}] the names by lower-case file name
    def self.track_titles
      @track_titles ||= NWConst::Library::BGM_SCENE_ITEMS.values.each_with_object({}) do |item, titles|
        titles[item[:file].to_s.downcase] = item[:name].to_s
      end
    end

    # Names one of the three actors in the active party with the most mastered jobs and races,
    # taking turns every ROTATION_SECONDS.
    #
    # @return [String, nil] the line, nil while nobody has mastered anything
    def self.top_master
      ranked = $game_party.battle_members.map { |actor| [actor, GameState.mastery(actor)] }
                          .select { |_, mastery| mastery.total > 0 }
                          .sort_by { |_, mastery| -mastery.total }.first(3)
      return nil if ranked.empty?

      actor, mastery = ranked[rotation % ranked.size]
      "#{actor.name} has mastered #{mastery.jobs} Jobs and #{mastery.races} Races already!"
    end

    # How many enemies were defeated, in this save or all saves.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.enemies_defeated
      count = Statistics[:defeat]
      "Has defeated #{NumberFormat.counted(count, 'enemy', 'enemies')}!" if count > 0
    end

    # How many battles were run away from, in this save or all saves.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.battles_escaped
      count = Statistics[:escape]
      "Has run away from #{NumberFormat.counted(count, 'battle')}!" if count > 0
    end

    # How often the party was wiped out, in this save or all saves.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.wipeouts
      count = Statistics[:lose]
      "Has been wiped out #{NumberFormat.counted(count, 'time')}!" if count > 0
    end

    # Names who in the active party leads in one stat, a different stat every ROTATION_SECONDS.
    #
    # Only that one stat is compared, and the result is kept until the rotation moves on.
    #
    # @return [String, nil] the line, nil with an empty party
    def self.top_stat
      slot = rotation
      game = $game_system.object_id
      return @top_stat if @top_stat_slot == slot && @top_stat_game == game

      @top_stat_slot = slot
      @top_stat_game = game

      param_id = slot % PARAM_COUNT
      best = $game_party.battle_members.max_by { |actor| actor.param(param_id) }
      @top_stat = best && "#{best.name} has the highest #{Vocab.param(param_id)} (#{NumberFormat.large(best.param(param_id))}) in the party!"
    end

    # The biggest hit dealt, in this save or all saves.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.biggest_hit
      damage = Statistics[:best_hit]
      "Biggest hit dealt: #{NumberFormat.large(damage)} damage!" if damage > 0
    end

    # How much gold was spent in shops, in this save or all saves.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.gold_spent
      gold = Statistics[:gold_spent]
      "Has spent #{NumberFormat.grouped(gold)} gold in shops!" if gold > 0
    end

    # How many items were synthesized, in this save or all saves.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.items_synthesized
      count = Statistics[:synthesize]
      "Has synthesized #{NumberFormat.counted(count, 'item')}!" if count > 0
    end

    # The deepest Labyrinth of Chaos floor reached.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.deepest_labyrinth_floor
      floor = $game_variables[NWConst::Var::EX_DUNGEON_REACH]
      "Has reached floor #{floor} in the Labyrinth of Chaos!" if floor > 0
    end

    # How many requests this save has made.
    #
    # @return [String, nil] the line, nil when it does not apply or the NSFW option is off
    def self.requests_made
      count = SaveStats[:requests]
      "Has made #{NumberFormat.counted(count, 'request')}!" if Options.nsfw? && count > 0
    end

    # Who this save has made the most requests to.
    #
    # @return [String, nil] the line, nil when it does not apply or the NSFW option is off
    def self.most_requested
      character, count = SaveStats.top(:requests)
      "Has requested #{character} the most, #{NumberFormat.counted(count, 'time')}!" if Options.nsfw? && character
    end

    # How many defeat scenes this save has seen.
    #
    # @return [String, nil] the line, nil when it does not apply or the NSFW option is off
    def self.times_raped
      count = SaveStats[:rapes]
      "Has been raped #{NumberFormat.counted(count, 'time')}!" if Options.nsfw? && count > 0
    end

    # Which monster girl this save has seen the most defeat scenes of.
    #
    # @return [String, nil] the line, nil when it does not apply or the NSFW option is off
    def self.most_raped_by
      monster, count = SaveStats.top(:rapes)
      "Raped by #{monster} the most, #{NumberFormat.counted(count, 'time')}!" if Options.nsfw? && monster
    end

    # How much gold the party carries.
    #
    # @return [String] the line
    def self.gold_carried
      "Currently carrying #{NumberFormat.grouped($game_party.gold)} gold!"
    end
  end

  # The counts behind the trivia, per save or across all saves as the statistics option says.
  module Statistics
    # The game's own count across all saves, by the SaveStats counter it matches. The requests and
    # defeat scenes have none, so they stay per save.
    ALL_SAVES = {
      :defeat     => :party_defeat,
      :escape     => :party_escape,
      :lose       => :party_lose,
      :synthesize => :party_synthesize,
      :gold_spent => :purchase_gold,
      :best_hit   => :party_damage_record_actor,
    }

    # @param key [Symbol] the SaveStats counter
    # @return [Integer] its count, from $game_library across all saves when the option says so
    def self.[](key)
      all_saves = ALL_SAVES[key]
      Options.all_saves? && all_saves ? $game_library.send(all_saves).to_i : SaveStats[key]
    end
  end

  # Counters the mod keeps per save: ones the game only keeps across all saves, the requests and
  # the defeat scenes.
  #
  # Kept in $game_system, so the game saves, loads, copies and backs them up with everything else,
  # and a new game starts them at 0. The game ignores them when a save is loaded without the mod.
  module SaveStats
    # Instance variable of $game_system that holds the counters.
    VARIABLE = :@mgq_discord_stats

    # Folder inside the mod folder where the earlier versions kept the counters, one file per save.
    LEGACY_DIR = "Stats"

    # The counters the earlier versions kept.
    LEGACY_KEYS = [:defeat, :escape, :lose, :synthesize, :gold_spent, :best_hit]

    # @param key [Symbol] the counter
    # @return [Integer] its value, 0 until it counted something
    def self.[](key)
      counts[key] || 0
    end

    # @param key [Symbol] the counter
    # @param amount [Integer] what to add
    def self.add(key, amount)
      counts[key] = self[key] + amount.to_i
    end

    # @param key [Symbol] the counter
    # @param value [Integer] a new candidate for the highest value
    def self.keep_highest(key, value)
      counts[key] = [self[key], value.to_i].max
    end

    # Counts one, in total and for a character.
    #
    # @param key [Symbol] the counter
    # @param character [String] the character
    def self.add_for(key, character)
      add(key, 1)
      tally(key)[character] = count_for(key, character) + 1
    end

    # @param key [Symbol] the counter
    # @param character [String] the character
    # @return [Integer] the count for them, 0 until the first
    def self.count_for(key, character)
      tally(key)[character] || 0
    end

    # @param key [Symbol] the counter
    # @return [Array(String, Integer), nil] the character with the highest count and that count, nil before the first
    def self.top(key)
      tally(key).max_by { |_, count| count }
    end

    # @param key [Symbol] the counter
    # @return [Hash{String => Integer}] its counts by character
    def self.tally(key)
      stored[:tallies][key] ||= {}
    end

    # @return [Hash{Symbol => Integer}] the totals by counter
    def self.counts
      stored[:counts]
    end

    # The counters of the loaded save, added to it on first use.
    #
    # @return [Hash] :counts with the totals, :tallies with the counts per character
    def self.stored
      $game_system.instance_variable_get(VARIABLE) ||
        $game_system.instance_variable_set(VARIABLE, { :counts => {}, :tallies => {} })
    end

    # Takes over the counters an earlier version kept in LEGACY_DIR for a save that was just loaded.
    #
    # Runs until the save holds counters of its own. A file whose fingerprint does not match was
    # written for another save in the same slot and is ignored.
    #
    # @param index [Integer] the save slot
    def self.import_legacy(index)
      return if $game_system.instance_variable_get(VARIABLE)

      path = MGQ_Discord.path("#{LEGACY_DIR}/#{File.basename(DataManager.make_filename(index), '.rvdata2')}.txt")
      return unless File.exist?(path)

      legacy = {}
      File.open(path, "rb") { |file| file.read }.each_line do |line|
        key, value = line.chomp.split("=", 2)
        legacy[key] = value if value
      end
      return unless legacy["fingerprint"] == "#{$game_system.save_count}:#{$game_system.instance_variable_get(:@frames_on_save)}"

      LEGACY_KEYS.each { |key| counts[key] = legacy[key.to_s].to_i }
    end
  end

  # The status as DiscordPresence.dll reads it, one key=value line per value.
  module StatusText
    # Builds the status.
    #
    # @param scene [String] what the game is showing, see GameState.scene
    # @return [String] the key=value lines
    def self.build(scene)
      leader = $game_party.leader rescue nil

      # Paradox keeps a personal level plus one for the job (class) and one for the race (tribe).
      fields = {
        "start"       => STARTED_AT,
        "last_input"  => MGQ_Discord.last_input_at,
        "scene"       => scene,
        "leader"      => MGQ_Discord.text_of { leader.name },
        "level"       => MGQ_Discord.text_of { leader.base_level },
        "class"       => MGQ_Discord.text_of { leader.class.name if leader },
        "class_level" => MGQ_Discord.text_of { leader.class_level },
        "race"        => MGQ_Discord.text_of { leader.tribe.name },
        "race_level"  => MGQ_Discord.text_of { leader.tribe_level },
        "area"        => GameState.area,
      }

      fields["vehicle"] = GameState.vehicle if scene == "travel"
      fields["overworld"] = 1 if (scene == "battle" || scene == "defeat_scene") && GameState.on_world_map?

      if scene == "request" && (character = Requests.current)
        fields["request_with"] = character
        fields["request_count"] = SaveStats.count_for(:requests, character)
      end

      if scene == "defeat_scene" && (monster = DefeatScenes.current)
        fields["raped_by"] = monster
        fields["raped_count"] = SaveStats.count_for(:rapes, monster)
      end

      if (labyrinth = GameState.labyrinth)
        fields["loc_floor"] = labyrinth.floor
        fields["loc_type"] = labyrinth.kind
        fields["loc_rare"] = NumberFormat.grouped(labyrinth.rare_points)
      end

      Trivia.lines.each_with_index { |line, index| fields["trivia#{index}"] = line } unless scene == "title"

      fields.map { |key, value| "#{key}=#{value.to_s.gsub(/[\r\n]/, ' ')}\n" }.join
    end
  end

  # Discord/DiscordPresence.dll, which talks to Discord on a thread of its own.
  module Presence
    # File name inside the mod folder.
    DLL = "DiscordPresence.dll"

    # @return [Boolean] whether the DLL is in the mod folder
    def self.installed?
      File.exist?(MGQ_Discord.path(DLL))
    end

    # Starts the DLL's thread. The DLL ignores a second start.
    def self.start
      function('presence_start', 'v').call
    end

    # Hands the DLL the latest status. Returns at once, the DLL does the rest on its own thread.
    #
    # @param status [String] the key=value lines
    def self.update(status)
      function('presence_update', 'p').call(status + "\0")
    end

    # @param name [String] the exported function
    # @param arguments [String] its arguments, in Win32API notation
    # @return [Win32API] the function, loaded once
    def self.function(name, arguments)
      @functions ||= {}
      @functions[name] ||= Win32API.new(MGQ_Discord.path(DLL), name, arguments, 'l')
    end
  end
end

MGQ_Discord.start

# Game hooks.
#
# Each wraps a game method: the original runs first, its result is returned unchanged, and the
# mod's part never raises. None is redefined by Plugins/*, recheck when the translation adds some.

if MGQ_Discord.hookable?
  begin
    MGQ_Discord::Options.register
  rescue => e
    MGQ_Discord::Log.write("options FAILED: #{e.class}: #{e.message}")
  end

  # Graphics.update runs every frame in every scene, so unlike a per-scene hook it cannot be missed.
  begin
    module Graphics
      class << self
        alias mgq_discord_update update
        def update
          mgq_discord_update
          MGQ_Discord.tick
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("Graphics hook FAILED: #{e.class}: #{e.message}")
  end

  # Every item use, in menus and in battle, passes item_apply once per target.
  begin
    class Game_Battler
      alias mgq_discord_item_apply item_apply
      def item_apply(user, item, *args)
        result = mgq_discord_item_apply(user, item, *args)
        begin
          MGQ_Discord::Trivia.item_used(item, self) if actor? && item.is_a?(RPG::Item)
        rescue
        end
        result
      end
    end
  rescue => e
    MGQ_Discord::Log.write("item_apply hook FAILED: #{e.class}: #{e.message}")
  end

  # Requests and defeat scenes play as novel scenes, and the Recollection Room replays them the same way.
  begin
    class Game_Novel
      alias mgq_discord_setup setup
      def setup(event_id)
        result = mgq_discord_setup(event_id)
        MGQ_Discord::DefeatScenes.forget rescue nil
        MGQ_Discord::Requests.started(event_id) rescue nil
        result
      end
    end
  rescue => e
    MGQ_Discord::Log.write("novel hook FAILED: #{e.class}: #{e.message}")
  end

  # A lost battle sets up its defeat scene here, after asking whether to skip it.
  begin
    module BattleManager
      class << self
        alias mgq_discord_change_novel_scene change_novel_scene
        def change_novel_scene(*args)
          result = mgq_discord_change_novel_scene(*args)
          MGQ_Discord::DefeatScenes.started rescue nil
          result
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("defeat scene hook FAILED: #{e.class}: #{e.message}")
  end

  # Saving leaves the mod's options out of the save file, and loading takes over the per-save
  # counters an earlier version kept for the save.
  begin
    module DataManager
      class << self
        alias mgq_discord_save_game_without_rescue save_game_without_rescue
        def save_game_without_rescue(index)
          MGQ_Discord::Options.left_out_of_save { mgq_discord_save_game_without_rescue(index) }
        end

        alias mgq_discord_load_game_without_rescue load_game_without_rescue
        def load_game_without_rescue(index)
          result = mgq_discord_load_game_without_rescue(index)
          begin
            MGQ_Discord::SaveStats.import_legacy(index)
          rescue => e
            MGQ_Discord::Log.write("stats import failed: #{e.class}: #{e.message}")
          end
          result
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("save/load hooks FAILED: #{e.class}: #{e.message}")
  end

  # Autosaves bypass save_game_without_rescue.
  begin
    module DataManager
      class << self
        alias mgq_discord_auto_save_game_without_rescue auto_save_game_without_rescue
        def auto_save_game_without_rescue(index)
          MGQ_Discord::Options.left_out_of_save { mgq_discord_auto_save_game_without_rescue(index) }
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("autosave hook FAILED: #{e.class}: #{e.message}")
  end

  # The backup save bypasses both.
  begin
    module DataManager
      class << self
        alias mgq_discord_save_game_backup_without_rescue save_game_backup_without_rescue
        def save_game_backup_without_rescue(*args)
          MGQ_Discord::Options.left_out_of_save { mgq_discord_save_game_backup_without_rescue(*args) }
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("backup save hook FAILED: #{e.class}: #{e.message}")
  end

  # The game's own counters across all saves, each also counted per save.
  begin
    class Game_Library
      [[:count_up_party_defeat,         :add,          :defeat,     false],
       [:count_up_party_escape,         :add,          :escape,     false],
       [:count_up_party_lose,           :add,          :lose,       false],
       [:count_up_party_synthesize,     :add,          :synthesize, false],
       [:addition_purchase_gold,        :add,          :gold_spent, true ],
       [:set_party_damage_record_actor, :keep_highest, :best_hit,   true ],
      ].each do |method, operation, key, amount_from_argument|
        original = :"mgq_discord_#{method}"
        alias_method original, method
        define_method(method) do |*args|
          result = send(original, *args)
          MGQ_Discord::SaveStats.send(operation, key, amount_from_argument ? args[0] : 1) rescue nil
          result
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("library hooks FAILED: #{e.class}: #{e.message}")
  end
end
