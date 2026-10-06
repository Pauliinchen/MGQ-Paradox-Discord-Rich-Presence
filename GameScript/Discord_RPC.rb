#----------------------------------------------------------------
#  Discord_RPC.rb
#
#  Changelog:
#      Paulinchen  2026-10-06: Wrote the in-game log as Discord InGame.log into the game folder's Logs folder
#      Paulinchen  2026-09-29: Found the mod folder relative to the game's folder, which works in a folder named with characters outside ASCII
#                            - Read the story, the trivia and who speaks in the untranslated game too
#                            - Let the invite box turn from ended to joined when the join comes late
#                            - Looked at the invite state again on the next title screen after a failed first look
#                            - Kept the title screen behind a box while Discord starts the game for an invite, and said when it had ended
#      Paulinchen  2026-09-28: Let other mods report their connection with a friend, take Discord invites and add status fields
#      Paulinchen  2026-09-27: Told of a newer release on the title screen, with an Update Check option to turn it off
#                            - Published the medals earned
#                            - Published the screen open in a menu
#                            - Handed the trivia to DiscordPresence.dll as values, which writes the sentences now
#                            - Added the monster queens recruited to the trivia of Part 2
#                            - Added the side taken at the Navy Headquarters to the trivia of Part 2
#                            - Added the routes cleared to the trivia of Part 3
#                            - Added the latest ore unlocked for forging to the trivia of Parts 1 and 2
#                            - Added the spirits recruited to the trivia of Parts 1 and 2
#                            - Added the Phenomena of Ruin defeated to the trivia of the Chaos route
#                            - Renamed the Picture and Shown Picture options to Activity Image and Shown Image
#                            - Added a Spoilers option that hides Part 3 spoilers
#                            - Added the Randolphs found to the trivia of Part 3
#                            - Grouped the trivia by the part and route they belong to
#                            - Hid the options that do not apply, and the ones under a greyed out option
#                            - Added Ilias / Alice (adult or sealed) and Routes (layered or logo) under a Dynamic Picture
#                            - Offered every picture of the application under Shown Picture
#                            - Showed the Destroyer and Judgment logos layered over their heroines
#                            - Called the Angelic Dominion and Monster Realm routes Destroyer and Judgment, after their logos
#                            - Added a Shown Picture option under Picture, the picture shown while Picture is Static
#                            - Grouped the options under a Rich Presence option that turns the whole status off
#                            - Published the act of the Collaboration Scenario while it is played
#                            - Published the part of the story, the side chosen and the route, and dropped the chosen side from the trivia
#                            - Told a Carnage run by the Labyrinth's type, which the Carnage floor counter never did
#                            - Named the monster girl of a defeat scene on 2.x too, which does not record her
#                            - Added a Picture option that shows Ilias or Alice, and later the route, instead of the app icon
#      Paulinchen  2026-09-26: Added the companions with the most affection to the trivia
#                            - Published who the player is talking to during a conversation
#                            - Added the battle fucks won to the trivia
#                            - Published a running battle fuck with the battlefucker
#                            - Published whether the camp music plays
#                            - Kept the per-save counters inside the save, taking over the earlier versions' files once
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

  # Logs every published status to Discord InGame.log.
  DEBUG = false

  # Folder next to Game.exe that holds everything the mod reads or writes but its logs.
  MOD_DIR = "Discord"

  # Folder next to Game.exe that holds the logs, shared with the user's other mods.
  LOG_DIR = "Logs"

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
  # @return [Boolean] false when the hooks are in place already, or the earlier version would add them.
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
  # @return [Boolean] Whether it is gone.
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
  # @return [Boolean] Whether the block is present, true when Patch.rb cannot be read.
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
    UpdateNotice.refresh
  rescue => e
    @frames = 0
    Log.write("tick failed: #{e.class}: #{e.message}")
  end

  # Publishes the status, unless it is unchanged since the last time.
  #
  # @param scene [String] What the game is showing, see GameState.scene.
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
  # @return [Integer] The time, the start of the game session until the first press.
  def self.last_input_at
    @last_input_at || STARTED_AT
  end

  # Builds the path of a file inside the mod folder, relative to the game's folder.
  #
  # The game always runs in its own folder. An absolute path breaks in a folder named with
  # characters outside ASCII, such as the untranslated game's: File takes it only as UTF-8,
  # Win32API loads a DLL only from the system's code page. A relative one, with backslashes, works
  # for both.
  #
  # @param name [String] The file name, relative to the mod folder.
  # @return [String] The path.
  def self.path(name)
    "#{MOD_DIR}\\#{name}"
  end

  # Builds the path of a log file in LOG_DIR, the way path builds one in the mod folder, and makes
  # the folder when it is missing.
  #
  # @param name [String] The log's file name.
  # @return [String] The path.
  def self.log_path(name)
    Dir.mkdir(LOG_DIR) unless File.directory?(LOG_DIR)
    "#{LOG_DIR}\\#{name}"
  end

  # Reads a value from the game as text.
  #
  # At the title screen $game_map exists but holds no map, so even display_name raises.
  #
  # @yieldreturn [Object] The value.
  # @return [String] The value as text, or "" when it is nil or reading it failed.
  def self.text_of
    value = yield
    value.nil? ? "" : value.to_s
  rescue
    ""
  end

  # Logs/Discord InGame.log, which only appears when something went wrong inside the game.
  module Log
    # Lines written per session at most, an error repeating every frame would flood the file.
    MAX_LINES = 60

    @lines = 0

    # Appends a line, prefixed with the time.
    #
    # @param message [String] The line to append.
    def self.write(message)
      return if @lines >= MAX_LINES
      @lines += 1

      File.open(MGQ_Discord.log_path("Discord InGame.log"), "ab") { |file| file.write("#{Time.now}  #{message}\n") }
    rescue
    end
  end

  # The mod's options, in the Mod Config Menu when it is installed, in the game's Config menu otherwise.
  #
  # The game keeps its options inside each save, but the mod's are the same for every save, so they
  # live in Discord/Settings.ini, are handed to whichever save is loaded and left out of the saves.
  module Options
    # File inside the mod folder, edited by players too.
    FILE = "Settings.ini"

    # What an option starts its name with in the menu, by how many options it sits under, as in
    # EXP Overlord.
    INDENTS = ["", "     ", "         -> "]

    # Whether Discord shows the game at all, 0 or 1. The other options sit under it in the menu.
    PRESENCE = :mod_discord_presence

    # Whether requests, defeat scenes and battle fucks show on Discord, 0 or 1.
    NSFW = :mod_discord_nsfw

    # Whether the trivia counts across all saves (1) or per save (0).
    ALL_SAVES = :mod_discord_all_saves

    # Whether Discord shows what spoils Part 3 (1) or leaves it out while it is played (0).
    SPOILERS = :mod_discord_spoilers

    # Whether the picture follows the story (1) or is always the app icon (0).
    PICTURE = :mod_discord_picture

    # Which picture shows while the picture is static: 0 the app icon, any other value one of
    # FIXED_PICTURES.
    SHOWN_PICTURE = :mod_discord_shown_picture

    # Whether the dynamic picture shows Ilias and Alice sealed (1) or in their adult forms (0).
    SEALED_SIDES = :mod_discord_sealed_sides

    # Whether the dynamic picture shows a route's logo over its heroines (1) or the logo alone (0).
    LAYERED_ROUTES = :mod_discord_layered_routes

    # Whether the title screen tells of a newer release of the mod (1) or GitHub is never asked (0).
    UPDATE_CHECK = :mod_discord_update_check

    # Art assets the Shown Image option can fix, by its value: every picture of the application.
    FIXED_PICTURES = {
      1  => "ilias_adult",
      2  => "ilias_sealed",
      3  => "alice_adult",
      4  => "alice_sealed",
      5  => "judgment",
      6  => "judgment_layered",
      7  => "destroyer",
      8  => "destroyer_layered",
      9  => "chaos",
      10 => "collab",
    }

    # Every option by its key in $game_system.conf, with its key in FILE.
    NAMES = {
      PRESENCE       => "presence",
      NSFW           => "nsfw",
      ALL_SAVES      => "all_saves",
      SPOILERS       => "spoilers",
      PICTURE        => "picture",
      SHOWN_PICTURE  => "shown_picture",
      SEALED_SIDES   => "sealed_sides",
      LAYERED_ROUTES => "layered_routes",
      UPDATE_CHECK   => "update_check",
    }

    # Every option, by its key in $game_system.conf.
    KEYS = NAMES.keys

    # How each option shows in the menu, its first value being the default.
    #
    # An option greys out while the one it sits under is off, shows only while that one holds its
    # :when value, and leaves the menu while that one is greyed out.
    MENU = {
      PRESENCE => {
        :name   => "[Discord] Rich Presence",
        :help   => "Show what you are doing in the game on your Discord profile.",
        :values => {
          1 => ["On",  "Discord shows where you are, what you are doing and trivia about your playthrough."],
          0 => ["Off", "Discord shows nothing about the game. The options below wait until it is on again."],
        },
      },
      NSFW => {
        :name   => "NSFW",
        :help   => "Show requests, defeat scenes and battle fucks on Discord.",
        :under  => PRESENCE,
        :values => {
          0 => ["Off", "Discord never mentions requests, defeat scenes or battle fucks."],
          1 => ["On",  "Discord shows a running request, defeat scene or battle fuck, and their counters."],
        },
      },
      ALL_SAVES => {
        :name   => "Statistics",
        :help   => "Count the trivia per save or across all saves.",
        :under  => PRESENCE,
        :values => {
          1 => ["All saves", "The game's own counts across every save."],
          0 => ["This save", "Counted by the mod for the loaded save, since the mod was installed."],
        },
      },
      SPOILERS => {
        :name   => "Spoilers",
        :help   => "Show names and trivia that spoil Part 3 on Discord.",
        :under  => PRESENCE,
        :values => {
          0 => ["Hide", "In Part 3, Discord leaves out spoiler trivia, names and places, and calls the routes Monster, Angel and Third."],
          1 => ["Show", "Discord shows everything, Part 3 included."],
        },
      },
      PICTURE => {
        :name   => "Activity Image",
        :help   => "Show the game's icon on Discord, or an image that follows the story.",
        :under  => PRESENCE,
        :values => {
          0 => ["Static",  "Always the same image, the game's icon unless Shown Image picks another."],
          1 => ["Dynamic", "Ilias or Alice, whoever you chose, and later the route you are on."],
        },
      },
      SHOWN_PICTURE => {
        :name   => "Shown Image",
        :help   => "The image shown while Activity Image is Static.",
        :under  => PICTURE,
        :when   => 0,
        :values => {
          0  => ["Default",                "The game's icon."],
          1  => ["Ilias (Adult)",          "Always Ilias in her adult form."],
          2  => ["Ilias (Sealed)",         "Always Ilias sealed."],
          3  => ["Alice (Adult)",          "Always Alice in her adult form."],
          4  => ["Alice (Sealed)",         "Always Alice sealed."],
          5  => ["Judgment (Logo)",        "Always the Judgment route's logo."],
          6  => ["Judgment (Layered)",     "Always the Judgment route's logo over its angels."],
          7  => ["Destroyer (Logo)",       "Always the Destroyer route's logo."],
          8  => ["Destroyer (Layered)",    "Always the Destroyer route's logo over its monster girls."],
          9  => ["Chaos",                  "Always the Chaos route's logo."],
          10 => ["Collaboration Scenario", "Always the collab's heroes."],
        },
      },
      SEALED_SIDES => {
        :name   => "Ilias / Alice",
        :help   => "How Ilias or Alice shows while Activity Image is Dynamic.",
        :under  => PICTURE,
        :when   => 1,
        :values => {
          1 => ["Sealed", "Ilias or Alice sealed."],
          0 => ["Adult",  "Ilias or Alice in her adult form."],
        },
      },
      LAYERED_ROUTES => {
        :name   => "Routes",
        :help   => "How the Judgment and Destroyer routes show while Activity Image is Dynamic.",
        :under  => PICTURE,
        :when   => 1,
        :values => {
          1 => ["Layered", "The route's logo over its heroines."],
          0 => ["Logo",    "The route's logo alone."],
        },
      },
      UPDATE_CHECK => {
        :name   => "Update Check",
        :help   => "Look for a new release of the mod when the game starts.",
        :under  => PRESENCE,
        :values => {
          1 => ["On",  "The title screen tells you when a new release is out. The game asks GitHub once per start."],
          0 => ["Off", "The game never asks GitHub for a new release."],
        },
      },
    }

    # Adds the options to the menu, each one under its parents, and keeps every entry, so arrange
    # can take out and put back the ones that do not apply.
    #
    # The Mod Config Menu's MOD_CONTENTS exists already, since the mod loader runs
    # 0_ModConfigMenu.rb before this script, and the game's own Config menu ignores :enable.
    def self.register
      config = NWConst::Config
      @menu = config.const_defined?(:MOD_CONTENTS) ? config::MOD_CONTENTS : config::CONTENTS
      @entries = []

      MENU.each do |key, option|
        entry = {
          :key  => key,
          :name => INDENTS[depth_of(key)] + option[:name],
          :sub  => true,
          :help => "#{option[:help]}\r\n←/→ Toggle",
        }
        entry[:enable] = proc { enabled?(key) } if option[:under]

        @entries << entry
        @menu.insert(-2, entry)
        config::DATA[key] = option[:values].keys
        config::DATA_TEXT[key] = {}
        option[:values].each { |value, (name, help)| config::DATA_TEXT[key][value] = { :name => name, :help => help } }
        config::DEFAULT[key] = option[:values].keys.first
      end

      arrange
    end

    # Puts the options that apply into the menu, below Rich Presence, which always shows, and takes
    # the others out. The config windows call it before they draw, so the menu follows every change.
    #
    # @return [Boolean] Whether the menu changed.
    def self.arrange
      return false unless @entries

      shown = @entries.select { |entry| shown?(entry[:key]) }
      listed = @menu.select { |item| @entries.any? { |entry| entry.equal?(item) } }
      return false if listed == shown

      at = @menu.index { |item| item.equal?(@entries.first) }
      @menu.reject! { |item| @entries.any? { |entry| entry.equal?(item) } }
      @menu.insert(at, *shown)
      true
    end

    # Counts the options an option sits under in the menu.
    #
    # @param key [Symbol] The option.
    # @return [Integer] How many options it sits under.
    def self.depth_of(key)
      parent = MENU[key][:under]
      parent ? depth_of(parent) + 1 : 0
    end

    # Tells whether an option is in the menu.
    #
    # @param key [Symbol] The option.
    # @return [Boolean] Whether it is in the menu as it currently shows.
    def self.shown?(key)
      option = MENU[key]
      parent = option[:under]
      return true unless parent

      shown?(parent) && enabled?(parent) && (!option.key?(:when) || in_menu(parent) == option[:when])
    end

    # Tells whether an option can be changed.
    #
    # @param key [Symbol] The option.
    # @return [Boolean] Whether it can be changed, rather than greyed out, as the menu currently shows.
    def self.enabled?(key)
      option = MENU[key]
      parent = option[:under]
      return true unless parent

      enabled?(parent) && (option.key?(:when) || in_menu(parent) == 1)
    end

    # Reads an option as the menu currently shows it, which may not be stored yet.
    #
    # @param key [Symbol] The option.
    # @return [Integer] Its value.
    def self.in_menu(key)
      value = $game_system.conf[key] rescue nil
      value.nil? ? self[key] : value
    end

    # Reads the Rich Presence option.
    #
    # @return [Boolean] Whether Discord shows the game at all.
    def self.presence?
      self[PRESENCE] == 1
    end

    # Reads the NSFW option.
    #
    # @return [Boolean] Whether requests, defeat scenes and battle fucks show on Discord.
    def self.nsfw?
      self[NSFW] == 1
    end

    # Reads the Statistics option.
    #
    # @return [Boolean] Whether the trivia counts across all saves instead of per save.
    def self.all_saves?
      self[ALL_SAVES] == 1
    end

    # Reads the Spoilers option.
    #
    # @return [Boolean] Whether Discord shows what spoils Part 3.
    def self.spoilers?
      self[SPOILERS] == 1
    end

    # Reads the Activity Image option.
    #
    # @return [Boolean] Whether the picture follows the story instead of being the app icon.
    def self.dynamic_picture?
      self[PICTURE] == 1
    end

    # Reads the Shown Image option.
    #
    # @return [String, nil] The art asset the Shown Image option picks, nil for the app icon.
    def self.fixed_picture
      FIXED_PICTURES[self[SHOWN_PICTURE]]
    end

    # Reads the Ilias / Alice option.
    #
    # @return [Boolean] Whether the dynamic picture shows Ilias and Alice sealed.
    def self.sealed_sides?
      self[SEALED_SIDES] == 1
    end

    # Reads the Routes option.
    #
    # @return [Boolean] Whether the dynamic picture shows a route's logo over its heroines.
    def self.layered_routes?
      self[LAYERED_ROUTES] == 1
    end

    # Reads the Update Check option.
    #
    # @return [Boolean] Whether the title screen tells of a newer release of the mod.
    def self.update_check?
      self[UPDATE_CHECK] == 1
    end

    # Reads an option's value.
    #
    # @param key [Symbol] The option.
    # @return [Integer] Its value, the menu's default until it was changed.
    def self.[](key)
      values.fetch(key) { NWConst::Config::DEFAULT[key] }
    end

    # Keeps the options of the loaded save and FILE in step, before every publish.
    #
    # Loading a save, starting a new game or returning to the title brings a new $game_system.conf,
    # which gets the stored options, so any other difference was made in the menu and gets stored.
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
    # @param keys [Array<Symbol>] The options to write.
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
    # @return [Hash{Symbol => Integer}] The values by option.
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

    # Reads the key of a line in FILE, compared without regard to case.
    #
    # @param line [String] The line.
    # @return [String, nil] The key in lower case, nil for a comment, a section header or a line without one.
    def self.name_of(line)
      line = line.strip
      return nil if line.start_with?("#", ";", "[")

      name, value = line.split("=", 2)
      value && name.strip.downcase
    end
  end

  # Large numbers the way the game writes them, which only the game can do. DiscordPresence.dll
  # formats every other number.
  module NumberFormat
    # Groups the thousands with commas: 1234567 becomes "1,234,567".
    #
    # @param number [Integer] The number.
    # @return [String] The grouped number.
    def self.grouped(number)
      number.to_i.to_s.reverse.scan(/\d{1,3}/).join(",").reverse
    end

    # Shortens a large number the way the game does: 28879000000000000 becomes "28.879Qdr.".
    #
    # give_unit comes from the game's LargeNumberConversion plugin.
    #
    # @param number [Integer] The number.
    # @return [String] The shortened number.
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

    # BGM file of the camp music, "Camping" in the jukebox's music room.
    CAMP_BGM = "yaei"

    # Value of "Labyrinth of Chaos: Type" during a Carnage run. A Normal run holds 9.
    CARNAGE_TYPE = 10

    # The untranslated game's names of the switches, variables and actors the mod reads by their
    # translated names.
    ORIGINAL_NAMES = {
      "Ilias Chosen"                  => "イリアス選択",
      "Alice Chosen"                  => "アリス選択",
      "Overall Events Progress"       => "総合イベント進行度",
      "Within Chaos Labyrinth"        => "混沌の迷宮内",
      "Labyrinth of Chaos: Type"      => "混沌迷宮の種類",
      "Chaos Labyrinth Current LV"    => "混沌の迷宮現在階層",
      "Labyrinth of Chaos Rare Value" => "混沌の迷宮レア値",
      "Support Pirates"               => "海賊に荷担",
      "Support Navy"                  => "海軍に荷担",
      "Angelic Dominion Route Clear"  => "天界ルートクリア",
      "Monster Realm Route Clear"     => "魔界ルートクリア",
      "Sylph"                         => "シルフ",
      "Gnome"                         => "ノーム",
      "Undine"                        => "ウンディーネ",
      "Salamander"                    => "サラマンダー",
    }.merge(Hash[(1..12).map { |act| ["Collab:C#{act}", "コラボ：C#{act}"] }])

    # Jobs and races an actor has taken to their maximum level.
    class Mastery < Struct.new(:jobs, :races)
      # Counts the jobs and races mastered.
      #
      # @return [Integer] Jobs and races together.
      def total
        jobs + races
      end
    end

    # Reports whether the player holds any button down.
    #
    # The game has no mouse support, so the mouse never counts.
    #
    # @return [Boolean] Whether a button is held down.
    def self.input?
      BUTTONS.any? { |button| Input.press?(button) }
    end

    # Reports whether a save is loaded.
    #
    # Before one is, the game holds placeholders: an empty party, no gold, map 0.
    #
    # @return [Boolean] Whether a save is loaded.
    def self.save_loaded?
      $game_map.map_id > 0
    rescue
      false
    end

    # Reads a switch by its name in the editor, so no id is hard-coded.
    #
    # @param name [String] The name of the switch.
    # @return [Boolean, nil] The switch, nil when no switch has that name.
    def self.switch_on?(name)
      id = id_of($data_system.switches, name)
      id && $game_switches[id]
    end

    # Finds every switch with a name in the editor, for names the game gives to a whole row of them.
    #
    # @param name [String] The name of the switches.
    # @return [Array<Integer>] Their ids, none when no switch has that name.
    def self.switches_named(name)
      $data_system.switches.each_index.select { |id| $data_system.switches[id] == name }
    end

    # Reads a variable by its name in the editor, so no id is hard-coded.
    #
    # @param name [String] The name of the variable.
    # @return [Integer] The value, 0 when no variable has that name.
    def self.variable(name)
      id = id_of($data_system.variables, name)
      id ? $game_variables[id].to_i : 0
    end

    # Finds a name in the editor's list of switches or variables, the untranslated game's too.
    #
    # @param names [Array<String>] The editor's names, by id.
    # @param name [String] The translated name.
    # @return [Integer, nil] The id, nil when neither name is in the list.
    def self.id_of(names, name)
      names.index(name) || (ORIGINAL_NAMES[name] && names.index(ORIGINAL_NAMES[name]))
    end

    # Tells whether a name from the game's data is the translated name or the untranslated game's.
    #
    # @param candidate [String] A name from the game's data.
    # @param name [String] The translated name.
    # @return [Boolean] Whether it is either.
    def self.named?(candidate, name)
      candidate == name || candidate == ORIGINAL_NAMES[name]
    end

    # Reports whether the party is inside the Labyrinth of Chaos.
    #
    # Its floors are named after their biome, so only the game's own flag tells.
    #
    # @return [Boolean] Whether the party is in the labyrinth.
    def self.in_labyrinth?
      switch_on?("Within Chaos Labyrinth") || variable("Within Chaos Labyrinth") > 0
    rescue
      false
    end

    # Reports whether the party is on the world map.
    #
    # The labyrinth's outdoor floors use the world map's field tilesets too.
    #
    # @return [Boolean] Whether the party is on the world map.
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
    # @return [String] "title", "battle", "request", "defeat_scene", "battlefuck", "travel", "map" or "menu".
    def self.scene
      scene = SceneManager.scene
      return "title" if scene.nil?

      name = scene.class.name.to_s
      return "battle"  if name =~ /Battle/
      return "title"   if name =~ /Title/
      return "request" if Options.nsfw? && Requests.current
      return "defeat_scene" if Options.nsfw? && DefeatScenes.current
      return "battlefuck" if Options.nsfw? && Battlefucks.current
      return "travel"  if on_world_map?
      return "map"    if name =~ /Map/
      "menu"
    rescue
      "map"
    end

    # Names the screen open in a menu, by the game's own class name, which DiscordPresence.dll
    # turns into what the player is doing there.
    #
    # @return [String] The class name, such as "Scene_Shop", or "" when unknown.
    def self.screen
      MGQ_Discord.text_of { SceneManager.scene.class.name }
    end

    # Reports whether the camp music plays, which the game starts while the party sets up camp.
    #
    # RPG::BGM.last is the game's own record of the music started last, emptied when it stops.
    #
    # @return [Boolean] Whether the camp music plays.
    def self.camping?
      File.basename(RPG::BGM.last.name.to_s, ".*").casecmp(CAMP_BGM) == 0
    rescue
      false
    end

    # Tells how the party crosses the world map.
    #
    # @return [String] "air", "sea" or "foot".
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
    # @return [String] The name, or "" when there is none.
    def self.area
      name = MGQ_Discord.text_of { $game_map.display_name }
      name = MGQ_Discord.text_of { $data_mapinfos[$game_map.map_id].name } if name.empty?
      name
    end

    # Reads the progress inside the Labyrinth of Chaos.
    #
    # Normal and Carnage runs count their floors in the same variable.
    #
    # @return [Labyrinth, nil] The progress, nil outside the labyrinth.
    def self.labyrinth
      return nil unless save_loaded? && in_labyrinth?

      carnage = variable("Labyrinth of Chaos: Type") == CARNAGE_TYPE

      Labyrinth.new(variable("Chaos Labyrinth Current LV"),
                    carnage ? "Carnage" : "Normal",
                    variable("Labyrinth of Chaos Rare Value"))
    end

    # Counts the jobs and races an actor has mastered.
    #
    # A job or race whose level in level_list reached its max_lv counts as mastered.
    #
    # @param actor [Game_Actor] The actor.
    # @return [Mastery] The mastered jobs and races.
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
    # @param event_id [Integer] The common event the scene plays.
    def self.started(event_id)
      character = character_of(event_id)
      SaveStats.add_for(:requests, character) if character
    end

    # Names the character of the running request.
    #
    # @return [String, nil] Who plays the running request, nil while none runs.
    def self.current
      $game_novel && $game_novel.running? ? character_of($game_novel.event_id) : nil
    end

    # Names who plays a request.
    #
    # The Recollection Room replays requests through the same novel scenes, with the game's
    # LIBRARY_H_MEMORY switch on.
    #
    # @param event_id [Integer] The common event a novel scene plays.
    # @return [String, nil] The character, nil for any other scene and for a replay.
    def self.character_of(event_id)
      return nil if $game_switches[NWConst::Sw::LIBRARY_H_MEMORY]

      characters[event_id]
    end

    # Characters by the common events of their requests, read from the Recollection Room once.
    #
    # A request is named after the companion who plays it, falling back to the Recollection Room's
    # entry without the form in brackets: "Alice (Small)" and "Alice (Adult)" both count as Alice.
    #
    # @return [Hash{Integer => String}] The characters by common event.
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
    # The name comes from the database, since looking her up in $game_actors would add her to the
    # save.
    #
    # @param item [Hash] The request's entry in the Recollection Room, unlocked at a companion's
    #   affection in variable ACTOR_REL_BASE + her actor id for most requests.
    # @return [String, nil] The companion's name, nil when the request unlocks otherwise.
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
    #
    # The Labyrinth of Chaos plays LOSE_EVENT_BASE for all its monsters, which is no monster girl's
    # scene.
    def self.started
      return if BattleManager.memory_battle? || $game_switches[NWConst::Sw::LIBRARY_H_MEMORY]

      event_id = $game_troop.lose_event_id
      enemy = winner(event_id)
      return if event_id <= NWConst::Common::LOSE_EVENT_BASE || enemy.nil? || skipped?(event_id)

      @running = [event_id, enemy.name.to_s]
      SaveStats.add_for(:rapes, enemy.name.to_s)
    end

    # Finds the monster girl who won the battle.
    #
    # 2.x does not record her when the battle starts, as 3.x does, but names her by the event's
    # offset from LOSE_EVENT_BASE, like its own encyclopedia.
    #
    # @param event_id [Integer] The common event of the defeat scene.
    # @return [RPG::Enemy, nil] The monster girl, nil when there is none.
    def self.winner(event_id)
      enemy_id = $game_temp.respond_to?(:lose_event_enemy_id) ? $game_temp.lose_event_enemy_id : event_id - NWConst::Common::LOSE_EVENT_BASE
      $data_enemies[enemy_id]
    end

    # Forgets the last defeat scene, so another novel scene with the same event is not taken for
    # it. Called from the Game_Novel#setup hook.
    def self.forget
      @running = nil
    end

    # Names the monster girl of the running defeat scene.
    #
    # @return [String, nil] The monster girl of the running defeat scene, nil while none runs.
    def self.current
      event_id, monster = @running
      monster if event_id && $game_novel && $game_novel.running? && $game_novel.event_id == event_id
    end

    # Reports whether the player chose to skip the scene.
    #
    # Skipping replaces the novel's event list with a copy that starts after the scene.
    #
    # @param event_id [Integer] The common event of the defeat scene.
    # @return [Boolean] Whether the scene was skipped.
    def self.skipped?(event_id)
      !$game_novel.interpreter.instance_variable_get(:@list).equal?($data_common_events[event_id].list)
    end
  end

  # Battle fucks: sex matches a battlefucker challenges Luka to. They play as a common event on the
  # map, not as a battle, and last until that common event returns, the scene after a win included.
  module Battlefucks
    # How the Recollection Room names the common event that starts a battle fuck.
    SCENE_NAME = "BF"

    # Remembers the battle fuck an interpreter is about to play. Called from the
    # Game_Interpreter#command_117 hook.
    #
    # @param interpreter [Game_Interpreter] The interpreter calling the common event.
    # @param event_id [Integer] The common event it calls.
    # @return [Boolean] Whether the common event starts a battle fuck.
    def self.starting(interpreter, event_id)
      return false if $game_switches[NWConst::Sw::LIBRARY_H_MEMORY]

      battlefucker = battlefuckers[event_id]
      @running = [battlefucker, interpreter, $game_map] if battlefucker
      !battlefucker.nil?
    end

    # Forgets the battle fuck once its common event returned. Called from the
    # Game_Interpreter#command_117 hook.
    def self.finished
      @running = nil
    end

    # Names the battlefucker of the running battle fuck.
    #
    # Loading a save or going back to the title abandons the interpreter without returning from the
    # common event, which the new $game_map and the stopped interpreter give away.
    #
    # @return [String, nil] The battlefucker, nil while none runs.
    def self.current
      battlefucker, interpreter, map = @running
      battlefucker if map && map.equal?($game_map) && interpreter.running?
    end

    # Battlefuckers by the common events starting their battle fucks, read from the Recollection
    # Room once, without the form in brackets: "Sara (Human)" counts as Sara.
    #
    # @return [Hash{Integer => String}] The battlefuckers by common event.
    def self.battlefuckers
      @battlefuckers ||= NWConst::Library::H_SCENE_ITEMS.values.each_with_object({}) do |character, names|
        name = character[:name].to_s.sub(/\s*[(（].*\z/m, "")

        (character[:items] || {}).each_value do |item|
          names[item[:common]] ||= name if item[:name].to_s == SCENE_NAME
        end
      end
    end
  end

  # Conversations: events the player starts on the map and novel scenes, named after who speaks.
  module Conversations
    # The Ace Message System's name box codes, \n<Name> and its placed variants, which the game
    # writes in front of a speaker's lines.
    NAME_BOX = /\\n[1-5cr]?<(.+?)>/i

    # The untranslated game's speaker line, the name in brackets opening a message: "【Name】".
    NAME_LINE = /\A【([^】]+)】/

    # What a companion's name box adds after the name: " (Affection:\V[3054])".
    AFFECTION_SUFFIX = /\s*[(（]Affection.*\z/i

    # Any other text code left in a name, such as \c[2].
    TEXT_CODE = /\\[a-z]+(\[[^\]]*\])?/i

    # Luka's name, as the translation's name boxes and the untranslated game's speaker lines write it.
    LUKA = ["Luka", "ルカ"]

    # Remembers who speaks in a message the game just queued.
    #
    # Luka speaks in most conversations, so he never replaces the one he is talking to.
    #
    # @param text [String] One line of the message.
    def self.heard(text)
      name = speaker_in(text)
      interpreter = running_interpreter
      return if name.nil? || LUKA.include?(name) || interpreter.nil?

      @speaker = [name, interpreter, interpreter.instance_variable_get(:@list)]
    end

    # Names who the player is talking to.
    #
    # A conversation lasts as long as the event that started it, whose list tells it apart from a
    # later event on the same interpreter, so the pauses between its messages count too.
    #
    # @return [String, nil] The last speaker other than Luka, nil outside a conversation.
    def self.current
      name, interpreter, list = @speaker
      name if interpreter && interpreter.equal?(running_interpreter) &&
              interpreter.instance_variable_get(:@list).equal?(list)
    end

    # The interpreter running the event on screen: the novel's in a novel scene, the map's on the map.
    #
    # Parallel events run on interpreters of their own, and battles on the troop's.
    #
    # @return [Game_Interpreter, nil] The interpreter, nil while it runs no event.
    def self.running_interpreter
      scene = SceneManager.scene
      interpreter = if scene.is_a?(Scene_Novel) then $game_novel.interpreter
                    elsif scene.is_a?(Scene_Map) then $game_map.interpreter
                    end
      interpreter if interpreter && interpreter.running?
    end

    # Reads the name in a line's name box, or of the untranslated game's speaker line.
    #
    # @param text [String] One line of a message.
    # @return [String, nil] The name, nil when the line names no speaker.
    def self.speaker_in(text)
      match = NAME_BOX.match(text.to_s) || NAME_LINE.match(text.to_s)
      return nil unless match

      name = match[1].sub(AFFECTION_SUFFIX, "").gsub(TEXT_CODE, "").strip
      name.empty? ? nil : name
    end
  end

  # Where the story stands: the part, the side this playthrough chose, in the final chapter the
  # route, and the act of the Collaboration Scenario while it is played. The keys of the route and
  # the collab double as the Discord application's art assets that the Activity Image option shows, the
  # routes' ones being their logos.
  module Story
    # Side chosen, by the switch that records the choice.
    SIDES = { "Ilias Chosen" => "ilias", "Alice Chosen" => "alice" }

    # Variable the story events raise at each checkpoint, from 0 at the intro to 40 at the Great
    # Decision.
    PROGRESS = "Overall Events Progress"

    # First progress of each part, latest first: the escape from Tartarus ends Part 1, the Great
    # Decision ends Part 2.
    PART_STARTS = [[3, 40], [2, 20], [1, 0]]

    # Route of the final chapter, by the variable that counts its progress.
    #
    # The Great Decision starts the counter of the route chosen, so a route not being played holds 0,
    # and 2.x has none of them.
    ROUTES = {
      "混沌ルート進行度" => "chaos",
      "天界ルート進行度" => "destroyer",
      "魔界ルート進行度" => "judgment",
    }

    # Art asset of Ilias or Alice in her adult form, by the side's key.
    ADULT_SIDES = { "ilias" => "ilias_adult", "alice" => "alice_adult" }

    # Art asset of Ilias or Alice sealed, by the side's key.
    SEALED_SIDES = { "ilias" => "ilias_sealed", "alice" => "alice_sealed" }

    # Art asset of a route's logo over its heroines, by the route's key. Chaos has none.
    LAYERED_ROUTES = { "destroyer" => "destroyer_layered", "judgment" => "judgment_layered" }

    # Art asset shown during the Collaboration Scenario.
    COLLAB = "collab"

    # Top folder of the editor's map tree that holds the Collaboration Scenario's maps.
    COLLAB_FOLDER = 920

    # Variables counting the progress of each act of the Collaboration Scenario, in order.
    COLLAB_ACTS = (1..12).map { |act| "Collab:C#{act}" }

    # Maps per block of the game's map folders: Data holds 1 to 999, Data/Map/Data 1001 to 1999.
    MAPS_PER_BLOCK = 1000

    # Names the art asset for the current point of the story.
    #
    # @return [String, nil] The collab during the Collaboration Scenario, the route in the final
    #   chapter, else the side, each in the form the options pick, nil before the side is chosen.
    def self.picture
      return COLLAB if collab_act

      if part == 3 && (chosen = route)
        Options.layered_routes? ? LAYERED_ROUTES.fetch(chosen, chosen) : chosen
      elsif (chosen = side)
        (Options.sealed_sides? ? SEALED_SIDES : ADULT_SIDES)[chosen]
      end
    end

    # Tells the act of the Collaboration Scenario being played.
    #
    # The end of Part 2 closes an unfinished Collaboration Scenario by filling in its last act, so
    # only its maps tell that it is being played.
    #
    # @return [Integer, nil] The act, 1 to 12, nil outside the Collaboration Scenario.
    def self.collab_act
      return nil unless top_folder($game_map.map_id) == COLLAB_FOLDER

      index = COLLAB_ACTS.rindex { |name| GameState.variable(name) > 0 }
      index && index + 1
    end

    # Finds the top folder of the editor's map tree that holds a map.
    #
    # Each block of map folders has a tree of its own, whose parent ids count from the block's start.
    #
    # @param map_id [Integer] The map.
    # @return [Integer] The top folder's map id, the map itself when it has no parent.
    def self.top_folder(map_id)
      block = map_id / MAPS_PER_BLOCK * MAPS_PER_BLOCK

      while (info = $data_mapinfos[map_id]) && info.parent_id > 0
        map_id = block + info.parent_id
      end

      map_id
    end

    # Reads the part the story is in.
    #
    # @return [Integer] The part the story is in, 1 to 3.
    def self.part
      progress = GameState.variable(PROGRESS)
      PART_STARTS.find { |_, start| progress >= start }[0]
    end

    # Reads the side this playthrough chose.
    #
    # @return [String, nil] The side this playthrough chose, nil before the choice.
    def self.side
      switch = SIDES.keys.find { |name| GameState.switch_on?(name) }
      SIDES[switch]
    end

    # Reads the route of the final chapter being played.
    #
    # @return [String, nil] The route of the final chapter being played, nil while none is.
    def self.route
      variable = ROUTES.keys.find { |name| GameState.variable(name) > 0 }
      ROUTES[variable]
    end

    # Reports whether Discord leaves out what spoils Part 3, which it does while Part 3 is played
    # unless the Spoilers option shows it.
    #
    # @return [Boolean] Whether spoilers are left out.
    def self.hides_spoilers?
      !Options.spoilers? && part == 3
    end
  end

  # The values behind the second Discord line. DiscordPresence.dll writes the sentences and picks
  # the ones that apply, so every value is published whatever the part, the route or the options.
  module Trivia
    # Seconds the values are kept before they are read again.
    RECOMPUTE_SECONDS = 5

    # Number of base parameters an actor has.
    PARAM_COUNT = 8

    # Companions the affection values name at most.
    TOP_AFFECTION_COUNT = 3

    # Actors the mastery values name at most.
    TOP_MASTER_COUNT = 3

    # Name in the editor of the switches that record each Randolph found, one per hiding place. 2.x
    # has none.
    RANDOLPH_FOUND = "親方発見"

    # The four spirits Luka recruits in Parts 1 and 2, by their names in the database.
    SPIRITS = ["Sylph", "Gnome", "Undine", "Salamander"]

    # Ores that unlock forging in Parts 1 and 2, by their item ids, weakest first: Iron, Gold,
    # Mithril, Crystal, Dragon Scale, Orichalcum, Rainbow Crystal and Meteorite, which 2.x lacks.
    #
    # The blacksmiths forge an ore's equipment while the party holds it, and forging never uses it up.
    FORGING_ORES = [151, 152, 153, 154, 155, 156, 157, 158]

    # Monster queens who join for good in Part 2, by their actor ids, the same in 2.x and 3.x: the Cow
    # Demon Queen, Miria, Antine Ann, Poseidoness, Candy, Airy, Freya, Alrauna, Lucretia, Laura,
    # Kraken, the Spider Princess, Fatima, and Lilith & Lilim. 3.x has second actors named like the
    # Cow Demon Queen and Fatima, so names would be ambiguous.
    MONSTER_QUEENS = [218, 245, 268, 280, 293, 315, 316, 322, 323, 328, 329, 334, 340, 341]

    # Side Luka takes at the Navy Headquarters in Part 2, by the switch that records the choice.
    NAVAL_SIDES = { "Support Pirates" => "pirates", "Support Navy" => "marines" }

    # Switches the game turns on when a route of the final chapter is cleared: Destroyer, Judgment,
    # Chaos. 2.x has none.
    ROUTE_CLEARS = ["Angelic Dominion Route Clear", "Monster Realm Route Clear", "混沌ルートクリア"]

    # Phenomena of Ruin the Chaos route sends the party after.
    PHENOMENA_OF_RUIN = 16

    # Variable holding the Phenomena of Ruin still left, despite its name ("number defeated"): the
    # Chaos route's prologue sets it to PHENOMENA_OF_RUIN and every defeat takes one off. 2.x has none.
    RUIN_LEFT = "十六の破滅事象撃破数"

    # Switch the game turns on when the last Phenomenon of Ruin falls, telling a RUIN_LEFT of 0 at
    # the end from the 0 it holds before the prologue.
    RUIN_ALL_DEFEATED = "図鑑フラグ：十六の破滅事象全撃破"

    # The last item an actor used, on whom and when.
    ItemUse = Struct.new(:item, :target, :used_at)

    # Every reader. Each returns its values by key, or nil while it has none.
    READERS = [
      :party, :affection, :progress, :last_item, :track, :masters, :top_stats, :statistics, :medals,
      :nsfw_counts, :spirits, :queens, :ore, :naval_side, :routes_cleared, :randolphs, :phenomena,
    ]

    # The values that currently apply.
    #
    # A reader that fails leaves out only its own values.
    #
    # @return [Hash{String => Object}] The values by key, none before a save is loaded.
    def self.values
      return {} unless GameState.save_loaded?

      now = Time.now.to_i
      game = $game_system.object_id
      return @values if @values && @game == game && now - @computed_at < RECOMPUTE_SECONDS

      @computed_at = now
      @game = game
      @values = READERS.each_with_object({}) do |reader, values|
        read = (send(reader) rescue nil)
        values.update(read) if read
      end
    end

    # Remembers an item an actor just used. Called from the Game_Battler#item_apply hook.
    #
    # @param item [RPG::Item] The item.
    # @param target [Game_Battler] The one it was used on.
    def self.item_used(item, target)
      @item_use = ItemUse.new(item.name.to_s, target.name.to_s, Time.now.to_i)
    end

    # The party: members down, companions recruited and gold carried.
    #
    # @return [Hash{String => Integer}] The values.
    def self.party
      {
        "dead_members" => $game_party.battle_members.count { |actor| actor.dead? },
        "companions"   => companions.size,
        "gold"         => $game_party.gold,
      }
    end

    # The recruited companions with the most affection, up to TOP_AFFECTION_COUNT.
    #
    # The game reads affection from $game_global_system, which all saves share.
    #
    # @return [Hash{String => Object}] Names and affection by rank, from 0.
    def self.affection
      ranked = companions.map { |actor| [actor.name, actor.actor.love.to_i] }
                         .select { |_, love| love > 0 }
                         .sort_by { |_, love| -love }.first(TOP_AFFECTION_COUNT)

      values = {}
      ranked.each_with_index do |(name, love), rank|
        values["affection_name#{rank}"] = name
        values["affection_love#{rank}"] = love
      end
      values
    end

    # Every companion who has joined, Luka left out.
    #
    # Read from the permanent roster, since include_members returns only the temporary party
    # during story sections like the Chaos domain.
    #
    # @return [Array<Game_Actor>] The companions.
    def self.companions
      $game_party.instance_variable_get(:@include_actors).map { |id| $game_actors[id] }.reject { |actor| actor.luca? }
    end

    # Battles fought, difficulty, playtime and the deepest Labyrinth of Chaos floor.
    #
    # @return [Hash{String => Integer}] The values.
    def self.progress
      {
        "battles"          => $game_system.battle_count,
        "difficulty"       => $game_variables[NWConst::Var::CURRENT_DIFFICULTY],
        "playtime_hours"   => $game_system.playtime / 3600,
        "labyrinth_record" => $game_variables[NWConst::Var::EX_DUNGEON_REACH],
      }
    end

    # The item an actor used last, on whom and when.
    #
    # @return [Hash{String => Object}, nil] The values, nil before the first use this session.
    def self.last_item
      use = @item_use
      use && { "item_used" => use.item, "item_target" => use.target, "item_used_at" => use.used_at }
    end

    # The music that is playing, named like in the music room of Kagetsumugi's jukebox.
    #
    # RPG::BGM.last is the game's own record of the music started last, emptied when it stops.
    #
    # @return [Hash{String => String}, nil] The title, nil when no music plays or the jukebox does
    #   not know the track.
    def self.track
      title = track_titles[File.basename(RPG::BGM.last.name.to_s, ".*").downcase]
      title && { "track" => title }
    end

    # Track names by BGM file name, read from the music room once.
    #
    # @return [Hash{String => String}] The names by lower-case file name.
    def self.track_titles
      @track_titles ||= NWConst::Library::BGM_SCENE_ITEMS.values.each_with_object({}) do |item, titles|
        titles[item[:file].to_s.downcase] = item[:name].to_s
      end
    end

    # The actors in the active party with the most mastered jobs and races, up to TOP_MASTER_COUNT.
    #
    # @return [Hash{String => Object}] Names, jobs and races by rank, from 0.
    def self.masters
      ranked = $game_party.battle_members.map { |actor| [actor, GameState.mastery(actor)] }
                          .select { |_, mastery| mastery.total > 0 }
                          .sort_by { |_, mastery| -mastery.total }.first(TOP_MASTER_COUNT)

      values = {}
      ranked.each_with_index do |(actor, mastery), rank|
        values["master_name#{rank}"] = actor.name
        values["master_jobs#{rank}"] = mastery.jobs
        values["master_races#{rank}"] = mastery.races
      end
      values
    end

    # Who in the active party leads in each stat.
    #
    # @return [Hash{String => String}] The stat's name, its leader and the value by stat, from 0.
    def self.top_stats
      (0...PARAM_COUNT).each_with_object({}) do |param_id, values|
        best = $game_party.battle_members.max_by { |actor| actor.param(param_id) }
        next unless best

        values["stat_name#{param_id}"] = Vocab.param(param_id)
        values["stat_holder#{param_id}"] = best.name
        values["stat_value#{param_id}"] = NumberFormat.large(best.param(param_id))
      end
    end

    # Defeats, escapes, wipeouts, syntheses, gold spent, the biggest hit and battle fucks won, in
    # this save or all saves.
    #
    # @return [Hash{String => Object}] The values.
    def self.statistics
      values = {
        "defeated"        => Statistics[:defeat],
        "escaped"         => Statistics[:escape],
        "wipeouts"        => Statistics[:lose],
        "synthesized"     => Statistics[:synthesize],
        "gold_spent"      => Statistics[:gold_spent],
        "battlefucks_won" => Statistics.battlefucks_won,
      }
      damage = Statistics[:best_hit]
      values["biggest_hit"] = NumberFormat.large(damage) if damage > 0
      values
    end

    # Requests made and defeat scenes seen in this save, in total and who the most.
    #
    # @return [Hash{String => Object}] The values.
    def self.nsfw_counts
      values = { "requests" => SaveStats[:requests], "rapes" => SaveStats[:rapes] }

      character, count = SaveStats.top(:requests)
      values.update("most_requested" => character, "most_requested_count" => count) if character

      monster, count = SaveStats.top(:rapes)
      values.update("most_raped_by" => monster, "most_raped_count" => count) if monster
      values
    end

    # How many of the game's medals, its achievements, have been earned.
    #
    # The game keeps them in $game_library, which all saves share, and leaves out the ones in
    # NO_USE_MEDAL, like its own Library does.
    #
    # @return [Hash{String => Integer}] The earned and the total.
    def self.medals
      valid = NWConst::Library::MEDAL_DATA.keys - NWConst::Library::NO_USE_MEDAL
      { "medals" => valid.count { |id| $game_library.has_medal?(id) }, "medals_total" => valid.size }
    end

    # How many of the four spirits have joined the party.
    #
    # @return [Hash{String => Integer}] The recruited and the total.
    def self.spirits
      { "spirits" => recruited_count(spirit_ids), "spirits_total" => spirit_ids.size }
    end

    # The actor ids of the SPIRITS, looked up once.
    #
    # @return [Array<Integer>] The ids, the first actor of each name.
    def self.spirit_ids
      @spirit_ids ||= SPIRITS.map { |name| $data_actors.index { |actor| actor && GameState.named?(actor.name, name) } }.compact
    end

    # How many of the monster queens have joined the party.
    #
    # @return [Hash{String => Integer}] The recruited and the total.
    def self.queens
      { "queens" => recruited_count(MONSTER_QUEENS), "queens_total" => MONSTER_QUEENS.size }
    end

    # Counts the actors that have joined the party.
    #
    # Read from the permanent roster, like companions.
    #
    # @param ids [Array<Integer>] The actor ids.
    # @return [Integer] How many of them joined.
    def self.recruited_count(ids)
      roster = $game_party.instance_variable_get(:@include_actors)
      ids.count { |id| roster.include?(id) }
    end

    # The best ore the party holds for forging, the one found last.
    #
    # @return [Hash{String => String}, nil] The ore's name, nil before the first ore.
    def self.ore
      ore = FORGING_ORES.reverse.map { |id| $data_items[id] }.find { |item| item && $game_party.has_item?(item) }
      ore && { "ore" => ore.name }
    end

    # Whether Luka sided with the pirates or the marines at the Navy Headquarters.
    #
    # @return [Hash{String => String}, nil] "pirates" or "marines", nil before the choice.
    def self.naval_side
      switch = NAVAL_SIDES.keys.find { |name| GameState.switch_on?(name) }
      switch && { "naval_side" => NAVAL_SIDES[switch] }
    end

    # How many routes of the final chapter this playthrough has cleared.
    #
    # @return [Hash{String => Integer}] The cleared and the total.
    def self.routes_cleared
      cleared = ROUTE_CLEARS.count { |name| GameState.switch_on?(name) }
      { "routes_cleared" => cleared, "routes_total" => ROUTE_CLEARS.size }
    end

    # How many of Randolph's hiding places this playthrough has found.
    #
    # @return [Hash{String => Integer}] The found and the total, which is 0 on 2.x.
    def self.randolphs
      found = randolph_switches.count { |id| $game_switches[id] }
      { "randolphs" => found, "randolphs_total" => randolph_switches.size }
    end

    # The switches recording each Randolph found, looked up once.
    #
    # @return [Array<Integer>] The switch ids, none on 2.x.
    def self.randolph_switches
      @randolph_switches ||= GameState.switches_named(RANDOLPH_FOUND)
    end

    # How many of the Phenomena of Ruin the Chaos route has defeated.
    #
    # @return [Hash{String => Integer}, nil] The defeated and the total, nil before the Chaos
    #   route's prologue or on 2.x.
    def self.phenomena
      left = GameState.variable(RUIN_LEFT)
      return nil if left <= 0 && !GameState.switch_on?(RUIN_ALL_DEFEATED)

      { "phenomena" => PHENOMENA_OF_RUIN - [left, 0].max, "phenomena_total" => PHENOMENA_OF_RUIN }
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

    # Reads a statistic, per save or across all saves as the Statistics option says.
    #
    # @param key [Symbol] The SaveStats counter.
    # @return [Integer] Its count, from $game_library across all saves when the option says so.
    def self.[](key)
      all_saves = ALL_SAVES[key]
      Options.all_saves? && all_saves ? $game_library.send(all_saves).to_i : SaveStats[key]
    end

    # The game counts battle fucks won per save itself, so unlike the others this count covers the
    # time before the mod was installed too.
    #
    # @return [Integer] The battle fucks won, from $game_library across all saves when the option says so.
    def self.battlefucks_won
      Options.all_saves? ? $game_library.battlefuck_win.to_i : $game_variables[NWConst::Var::BATTLEFUCKER_DEFEAT].to_i
    end
  end

  # Counters the mod keeps per save: ones the game only keeps across all saves, the requests and
  # the defeat scenes.
  #
  # Kept in $game_system, so the game saves, loads, copies and backs them up with everything else,
  # and ignores them when a save is loaded without the mod.
  module SaveStats
    # Instance variable of $game_system that holds the counters.
    VARIABLE = :@mgq_discord_stats

    # Folder inside the mod folder where the earlier versions kept the counters, one file per save.
    LEGACY_DIR = "Stats"

    # The counters the earlier versions kept.
    LEGACY_KEYS = [:defeat, :escape, :lose, :synthesize, :gold_spent, :best_hit]

    # Reads a per-save counter.
    #
    # @param key [Symbol] The counter.
    # @return [Integer] Its value, 0 until it counted something.
    def self.[](key)
      counts[key] || 0
    end

    # Adds to a per-save counter.
    #
    # @param key [Symbol] The counter.
    # @param amount [Integer] What to add.
    def self.add(key, amount)
      counts[key] = self[key] + amount.to_i
    end

    # Keeps the highest value a per-save counter was offered.
    #
    # @param key [Symbol] The counter.
    # @param value [Integer] A new candidate for the highest value.
    def self.keep_highest(key, value)
      counts[key] = [self[key], value.to_i].max
    end

    # Counts one, in total and for a character.
    #
    # @param key [Symbol] The counter.
    # @param character [String] The character.
    def self.add_for(key, character)
      add(key, 1)
      tally(key)[character] = count_for(key, character) + 1
    end

    # Reads a per-save counter for one character.
    #
    # @param key [Symbol] The counter.
    # @param character [String] The character.
    # @return [Integer] The count for them, 0 until the first.
    def self.count_for(key, character)
      tally(key)[character] || 0
    end

    # Finds the character a per-save counter counted most.
    #
    # @param key [Symbol] The counter.
    # @return [Array(String, Integer), nil] The character with the highest count and that count, nil before the first.
    def self.top(key)
      tally(key).max_by { |_, count| count }
    end

    # Reads a per-save counter for every character.
    #
    # @param key [Symbol] The counter.
    # @return [Hash{String => Integer}] Its counts by character.
    def self.tally(key)
      stored[:tallies][key] ||= {}
    end

    # Reads every per-save counter.
    #
    # @return [Hash{Symbol => Integer}] The totals by counter.
    def self.counts
      stored[:counts]
    end

    # The counters of the loaded save, added to it on first use.
    #
    # @return [Hash] :counts with the totals, :tallies with the counts per character.
    def self.stored
      $game_system.instance_variable_get(VARIABLE) ||
        $game_system.instance_variable_set(VARIABLE, { :counts => {}, :tallies => {} })
    end

    # Takes over the counters an earlier version kept in LEGACY_DIR for a save that was just loaded.
    #
    # A file whose fingerprint does not match was written for another save in the same slot.
    #
    # @param index [Integer] The save slot.
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
    # The whole status while the Rich Presence option is off, which clears the profile.
    HIDDEN = "hidden=1\n"

    # Builds the status.
    #
    # @param scene [String] What the game is showing, see GameState.scene.
    # @return [String] The key=value lines.
    def self.build(scene)
      return HIDDEN unless Options.presence?

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
      fields["screen"] = GameState.screen if scene == "menu"
      fields["overworld"] = 1 if (scene == "battle" || scene == "defeat_scene") && GameState.on_world_map?
      fields["camping"] = 1 if GameState.camping?

      if scene == "request" && (character = Requests.current)
        fields["request_with"] = character
        fields["request_count"] = SaveStats.count_for(:requests, character)
      end

      if scene == "defeat_scene" && (monster = DefeatScenes.current)
        fields["raped_by"] = monster
        fields["raped_count"] = SaveStats.count_for(:rapes, monster)
      end

      fields["battlefuck_with"] = Battlefucks.current if scene == "battlefuck"

      talking_to = MGQ_Discord.text_of { Conversations.current }
      fields["talking_to"] = talking_to unless talking_to.empty?

      picture = MGQ_Discord.text_of { Options.dynamic_picture? ? Story.picture : Options.fixed_picture }
      fields["picture"] = picture unless picture.empty?

      if GameState.save_loaded?
        collab_act = MGQ_Discord.text_of { Story.collab_act }
        fields["collab_act"] = collab_act unless collab_act.empty?

        part = MGQ_Discord.text_of { Story.part }
        fields["part"] = part unless part.empty?
        fields["side"] = MGQ_Discord.text_of { Story.side }
        fields["route"] = MGQ_Discord.text_of { Story.route } if part == "3"
        fields["hide_spoilers"] = 1 if (Story.hides_spoilers? rescue false)
      end

      if (labyrinth = GameState.labyrinth)
        fields["loc_floor"] = labyrinth.floor
        fields["loc_type"] = labyrinth.kind
        fields["loc_rare"] = labyrinth.rare_points
      end

      fields["nsfw"] = 1 if Options.nsfw?
      fields.update(Trivia.values) unless scene == "title"
      fields.update(Bridge.status_fields(scene))

      fields.map { |key, value| "#{key}=#{value.to_s.gsub(/[\r\n]/, ' ')}\n" }.join
    end
  end

  # Two lines on the title screen, below the translation's version, once a newer release of the mod is out.
  module UpdateNotice
    # What the notice says, the newer version filled in.
    LINES = [
      "Discord Rich Presence %s is out.",
      "Close the game and run Discord\\Update.bat to update.",
    ]

    # Height of a line, which the font size follows.
    LINE_HEIGHT = 20

    # Where the notice starts, below the translation's version.
    TOP = 24

    # Gap to the left and right edges of the screen, as the translation's version keeps it.
    MARGIN = 4

    # Layer of the title screen's foreground, which the notice belongs to.
    Z = 100

    # Shows the notice on the title screen once the DLL found a newer release, and asks the DLL to
    # look for one the first time the title screen shows. Called every publish.
    def self.refresh
      return if @sprite || !SceneManager.scene.is_a?(Scene_Title)
      return unless Options.presence? && Options.update_check?

      @asked ||= Presence.check_for_update == 1
      version = Presence.newer_version
      show(version) unless version.empty?
    end

    # Takes the notice off the screen. Called when the title screen ends.
    def self.hide
      return unless @sprite

      @sprite.bitmap.dispose
      @sprite.dispose
      @sprite = nil
    end

    # Draws the notice.
    #
    # @param version [String] The newer release's version.
    def self.show(version)
      @sprite = Sprite.new
      @sprite.bitmap = Bitmap.new(Graphics.width, LINE_HEIGHT * LINES.size)
      @sprite.bitmap.font.size = LINE_HEIGHT
      @sprite.y = TOP
      @sprite.z = Z

      LINES.each_with_index do |line, index|
        @sprite.bitmap.draw_text(MARGIN, index * LINE_HEIGHT, Graphics.width - 2 * MARGIN, LINE_HEIGHT, format(line, version))
      end
    end
  end

  # A box on the title screen while Discord starts the game for an invite. It keeps the player from
  # choosing anything until Discord hands over the join, which another mod such as Monster Girl
  # Quest! Online joins with, or says that the invite had ended.
  module InviteBox
    # Discord did not start the game, as the DLL reports it.
    NONE = 0

    # Discord started the game, and its join has not come yet.
    WAITING = 1

    # Discord handed over the join.
    JOINED = 2

    # No join came in time, so the invite had ended.
    ENDED = 3

    # What the box says in each state.
    TEXTS = {
      WAITING => ["Joining your friend through Discord . . .", "Waiting for Discord to hand over the invite."],
      JOINED => ["Joining your friend through Discord . . .", "Discord handed over the invite."],
      ENDED => ["The Discord invite you accepted has ended.", "Ask your friend for a new one. (Enter)"],
    }

    # Width of the box.
    WIDTH = 480

    # Height of the box, two lines.
    HEIGHT = 72

    # Layer above the title screen's windows.
    Z = 250

    # Frames between two looks at the DLL.
    CHECK_FRAMES = 10

    # Frames the box waits after the join for another mod to take it, ten seconds, before it lets the
    # player go on, since without such a mod nothing else happens.
    HANDOFF_FRAMES = 600

    # Opens the box when Discord started the game, the first time the title screen shows. A failed
    # look at the DLL leaves it to the next title screen.
    def self.open
      return if @done

      @state = Presence.invite_state
      @done = true
      return if @state == NONE

      @window = Window_Base.new((Graphics.width - WIDTH) / 2, (Graphics.height - HEIGHT) / 2, WIDTH, HEIGHT)
      @window.z = Z
      @frames = 0
      draw
    end

    # Tells whether the box keeps the player from choosing anything.
    #
    # @return [Boolean] Whether the box is open.
    def self.blocking?
      @window ? true : false
    end

    # Follows the DLL's state, and closes the box once the player read that the invite had ended, or
    # when no other mod took the join. Called every frame while the box is open.
    def self.update
      @frames += 1
      follow_state if @frames % CHECK_FRAMES == 0
      return unless @window

      if @state == ENDED
        close if Input.trigger?(:C) || Input.trigger?(:B)
      elsif @state == JOINED && @frames >= HANDOFF_FRAMES
        close
      end
    end

    # Takes the DLL's state when it changed, and closes the box when Discord did not start the game
    # after all. An invite shown as ended only changes to a join that came late.
    def self.follow_state
      state = Presence.invite_state
      return if state == @state || (@state == ENDED && state != JOINED)

      @state = state
      @frames = 0
      return close if @state == NONE

      draw
    end

    # Closes the box. Called when the title screen ends too.
    def self.close
      @window.dispose if @window && !@window.disposed?
      @window = nil
    end

    # Writes the state's text into the box.
    def self.draw
      @window.contents.clear
      TEXTS.fetch(@state, TEXTS[WAITING]).each_with_index do |line, index|
        @window.contents.draw_text(0, index * @window.line_height, @window.contents.width, @window.line_height, line, 1)
      end
    end
  end

  # What other mods, such as Monster Girl Quest! Online, hand the presence: their connection with a friend,
  # which the activity shows and Discord invites to, and fields of their own for the status; and what
  # they take from it: invites the player accepted in Discord, and the player's name on Discord. They
  # check VERSION first, so this mod never needs to know them.
  module Bridge
    # Changes whenever a call's meaning changes.
    VERSION = 1

    # Bytes the DLL may write a join secret or the player's name into, its terminating null included.
    TEXT_SIZE = 256

    # Tells whether the calls reach Discord.
    #
    # @return [Boolean] Whether the presence runs, so the calls reach Discord.
    def self.available?
      ENABLED && Presence.installed?
    end

    # Reports that the player hosts and waits for a friend, whom Discord can invite.
    #
    # @param party [String] Names the party, the same for both players.
    # @param join_secret [String] What a friend who joins gets, at most 128 characters.
    def self.hosting(party, join_secret)
      report("hosting", party, join_secret, "")
    end

    # Reports that the player plays with a friend.
    #
    # @param party [String] Names the party, the same for both players.
    # @param friend [String] The friend's name.
    def self.connected(party, friend)
      report("connected", party, "", friend)
    end

    # Reports that the player neither hosts nor plays with a friend.
    def self.idle
      report("", "", "", "")
    end

    # Takes the join secret of an invite the player accepted in Discord.
    #
    # @return [String, nil] The join secret of an invite the player accepted in Discord, handed out once.
    def self.take_invite
      text = read('presence_take_invite')
      text.empty? ? nil : text
    end

    # Reads the player's name on Discord.
    #
    # @return [String, nil] The player's name on Discord, nil until Discord told it.
    def self.player_name
      name = read('presence_player_name')
      name.empty? ? nil : name
    end

    # Adds fields to every status, which DiscordPresence.dll turns into what Discord shows.
    #
    # @yieldparam scene [String] What the game is showing, see GameState.scene.
    # @yieldreturn [Hash, nil] Field names and values.
    def self.add_status(&source)
      (@sources ||= []) << source
    end

    # Collects the fields the registered sources add to the status.
    #
    # @param scene [String] What the game is showing, see GameState.scene.
    # @return [Hash] The fields every source adds, a failing source left out.
    def self.status_fields(scene)
      (@sources || []).inject({}) do |fields, source|
        begin
          fields.merge(source.call(scene) || {})
        rescue => e
          Log.write("bridge status source failed: #{e.class}: #{e.message}")
          fields
        end
      end
    end

    # Hands the DLL the connection another mod reports.
    #
    # @param kind [String] "hosting", "connected" or "" for none.
    # @param party [String] Names the party.
    # @param join_secret [String] What a friend who joins gets.
    # @param friend [String] The friend's name.
    def self.report(kind, party, join_secret, friend)
      return unless available?

      Presence.function('presence_set_connection', 'pppp').call(kind + "\0", party.to_s + "\0", join_secret.to_s + "\0", friend.to_s + "\0")
    rescue => e
      Log.write("bridge report failed: #{e.class}: #{e.message}")
    end

    # Reads a text an export writes into a buffer.
    #
    # @param name [String] An export that writes a text into a buffer.
    # @return [String] The text, "" when there is none.
    def self.read(name)
      return "" unless available?

      buffer = "\0" * TEXT_SIZE
      length = Presence.function(name, 'pl').call(buffer, buffer.size)
      # The DLL wrote bytes past Ruby's back, so only a binary string counts them right.
      length > 0 ? buffer.force_encoding("ASCII-8BIT")[0, length].force_encoding("UTF-8") : ""
    rescue => e
      Log.write("bridge read failed: #{e.class}: #{e.message}")
      ""
    end
  end

  # Discord/DiscordPresence.dll, which talks to Discord on a thread of its own.
  module Presence
    # File name inside the mod folder.
    DLL = "DiscordPresence.dll"

    # Bytes the DLL may write a version into, its terminating null included.
    VERSION_SIZE = 32

    # Tells whether the DLL is installed.
    #
    # @return [Boolean] Whether the DLL is in the mod folder.
    def self.installed?
      File.exist?(MGQ_Discord.path(DLL))
    end

    # Starts the DLL's thread. The DLL ignores a second start.
    def self.start
      function('presence_start', 'v').call
    end

    # Hands the DLL the latest status. Returns at once, the DLL does the rest on its own thread.
    #
    # @param status [String] The key=value lines.
    def self.update(status)
      function('presence_update', 'p').call(status + "\0")
    end

    # Has the DLL ask GitHub for a newer release on a thread of its own. The DLL ignores a second call.
    #
    # @return [Integer] 1 when the DLL started asking, 0 when it failed.
    def self.check_for_update
      function('presence_check_for_update', 'v').call
    end

    # Reads the version of a newer release, once the DLL found one.
    #
    # @return [String] The newer release's version the DLL found, "" while it knows of none.
    def self.newer_version
      buffer = "\0" * VERSION_SIZE
      length = function('presence_newer_version', 'pl').call(buffer, buffer.size)
      buffer[0, length]
    end

    # Asks the DLL where a game Discord started for an invite stands.
    #
    # @return [Integer] One of InviteBox's states, NONE when Discord did not start the game.
    def self.invite_state
      function('presence_invite_state', 'v').call
    end

    # Loads an export of the DLL.
    #
    # @param name [String] The exported function.
    # @param arguments [String] Its arguments, in Win32API notation.
    # @return [Win32API] The function, loaded once.
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

  # The config windows draw every option again after each change, so the options that a change
  # shows or hides come and go right away. The game's window sizes its contents to the options once,
  # the Mod Config Menu's its width, so both are measured again when the options change.
  begin
    [:Window_Config, :Window_ModConfig].select { |name| Object.const_defined?(name) }.each do |name|
      Object.const_get(name).class_eval do
        alias_method :mgq_discord_refresh, :refresh
        define_method(:refresh) do
          if (MGQ_Discord::Options.arrange rescue false)
            calculate_and_resize if respond_to?(:calculate_and_resize)
            create_contents
          end
          mgq_discord_refresh
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("config window hooks FAILED: #{e.class}: #{e.message}")
  end

  begin
    module Graphics
      class << self
        alias mgq_discord_update update

        # Draws the frame, then runs the presence's tick.
        #
        # Graphics.update runs every frame in every scene, so unlike a per-scene hook it cannot be
        # missed.
        def update
          mgq_discord_update
          MGQ_Discord.tick
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("Graphics hook FAILED: #{e.class}: #{e.message}")
  end

  begin
    class Scene_Title
      alias mgq_discord_start start

      # Starts the title screen, then opens the invite box when Discord started the game.
      def start
        mgq_discord_start
        begin
          MGQ_Discord::InviteBox.open if MGQ_Discord::Presence.installed?
        rescue => e
          MGQ_Discord::Log.write("invite box failed: #{e.class}: #{e.message}")
        end
      end

      alias mgq_discord_update update

      # Updates the title screen, or only the screen and the invite box while the box is open, which
      # keeps the command window from taking any input.
      def update
        return mgq_discord_update unless MGQ_Discord::InviteBox.blocking?

        Graphics.update
        Input.update
        begin
          MGQ_Discord::InviteBox.update
        rescue => e
          MGQ_Discord::InviteBox.close
          MGQ_Discord::Log.write("invite box failed: #{e.class}: #{e.message}")
        end
      end

      alias mgq_discord_terminate terminate

      # Ends the title screen and takes the update notice and the invite box off with its sprites.
      def terminate
        mgq_discord_terminate
      ensure
        MGQ_Discord::UpdateNotice.hide rescue nil
        MGQ_Discord::InviteBox.close rescue nil
      end
    end
  rescue => e
    MGQ_Discord::Log.write("title hook FAILED: #{e.class}: #{e.message}")
  end

  begin
    class Game_Battler
      alias mgq_discord_item_apply item_apply

      # Applies an item or skill, then counts an item a party member used.
      #
      # Every item use, in menus and in battle, passes here once per target.
      #
      # @param user [Game_Battler] Who uses it.
      # @param item [RPG::UsableItem] The item or skill.
      # @param args [Array] The game's further arguments.
      # @return [Object] The original's result.
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

  begin
    class Game_Novel
      alias mgq_discord_setup setup

      # Sets up a novel scene, then forgets the last defeat scene and notes a request that starts.
      #
      # Requests and defeat scenes play as novel scenes, and the Recollection Room replays them the
      # same way.
      #
      # @param event_id [Integer] The common event the scene plays.
      # @return [Object] The original's result.
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

  begin
    class Game_Message
      alias mgq_discord_add add

      # Adds a line to the message, then notes who speaks.
      #
      # Every line a message shows passes here, the speaker's name box included, on the map and in
      # novel scenes.
      #
      # @param text [String] The line.
      # @return [Object] The original's result.
      def add(text)
        result = mgq_discord_add(text)
        MGQ_Discord::Conversations.heard(text) rescue nil
        result
      end
    end
  rescue => e
    MGQ_Discord::Log.write("message hook FAILED: #{e.class}: #{e.message}")
  end

  begin
    class Game_Interpreter
      alias mgq_discord_command_117 command_117

      # Calls a common event, and notes a battle fuck while it runs.
      #
      # A map event calls the common event of a battle fuck, which returns once the battle fuck is
      # over.
      #
      # @return [Object] The original's result.
      def command_117
        started = MGQ_Discord::Battlefucks.starting(self, @params[0]) rescue false
        mgq_discord_command_117
      ensure
        MGQ_Discord::Battlefucks.finished if started
      end
    end
  rescue => e
    MGQ_Discord::Log.write("battle fuck hook FAILED: #{e.class}: #{e.message}")
  end

  begin
    module BattleManager
      class << self
        alias mgq_discord_change_novel_scene change_novel_scene

        # Sets up a lost battle's defeat scene, after asking whether to skip it, then notes it.
        #
        # @param args [Array] The game's arguments.
        # @return [Object] The original's result.
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

  begin
    module DataManager
      class << self
        alias mgq_discord_save_game_without_rescue save_game_without_rescue

        # Saves the game with the mod's options left out of the save file.
        #
        # @param index [Integer] The save slot.
        # @return [Object] The original's result.
        def save_game_without_rescue(index)
          MGQ_Discord::Options.left_out_of_save { mgq_discord_save_game_without_rescue(index) }
        end

        alias mgq_discord_load_game_without_rescue load_game_without_rescue

        # Loads a save, then takes over the per-save counters an earlier version kept for it.
        #
        # @param index [Integer] The save slot.
        # @return [Object] The original's result.
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

  begin
    module DataManager
      class << self
        alias mgq_discord_auto_save_game_without_rescue auto_save_game_without_rescue

        # Autosaves with the mod's options left out of the save file.
        #
        # Autosaves bypass save_game_without_rescue.
        #
        # @param index [Integer] The save slot.
        # @return [Object] The original's result.
        def auto_save_game_without_rescue(index)
          MGQ_Discord::Options.left_out_of_save { mgq_discord_auto_save_game_without_rescue(index) }
        end
      end
    end
  rescue => e
    MGQ_Discord::Log.write("autosave hook FAILED: #{e.class}: #{e.message}")
  end

  begin
    module DataManager
      class << self
        alias mgq_discord_save_game_backup_without_rescue save_game_backup_without_rescue

        # Writes the backup save with the mod's options left out of it.
        #
        # The backup save bypasses save_game_without_rescue and the autosave.
        #
        # @param args [Array] The game's arguments.
        # @return [Object] The original's result.
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
