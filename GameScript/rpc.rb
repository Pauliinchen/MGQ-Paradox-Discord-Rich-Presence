#----------------------------------------------------------------
#  rpc.rb
#
#  Changelog:
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

  # Publishes the status every PUBLISH_INTERVAL frames. Called once per frame.
  def self.tick
    return unless @running

    @frames = (@frames || 0) + 1
    return if @frames < PUBLISH_INTERVAL

    @frames = 0
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

    # Jobs and races an actor has taken to their maximum level.
    class Mastery < Struct.new(:jobs, :races)
      # @return [Integer] jobs and races together
      def total
        jobs + races
      end
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
    # @return [String] "title", "battle", "travel", "map" or "menu"
    def self.scene
      scene = SceneManager.scene
      return "title" if scene.nil?

      name = scene.class.name.to_s
      return "battle" if name =~ /Battle/
      return "title"  if name =~ /Title/
      return "travel" if on_world_map?
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
      :top_master,
      :enemies_defeated,
      :battles_escaped,
      :wipeouts,
      :top_stat,
      :biggest_hit,
      :gold_spent,
      :items_synthesized,
      :deepest_labyrinth_floor,
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

    # How many enemies this save has defeated.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.enemies_defeated
      count = SaveStats[:defeat]
      "Has defeated #{NumberFormat.counted(count, 'enemy', 'enemies')}!" if count > 0
    end

    # How many battles this save has run away from.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.battles_escaped
      count = SaveStats[:escape]
      "Has run away from #{NumberFormat.counted(count, 'battle')}!" if count > 0
    end

    # How often this save's party was wiped out.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.wipeouts
      count = SaveStats[:lose]
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

    # The biggest hit this save has dealt.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.biggest_hit
      damage = SaveStats[:best_hit]
      "Biggest hit dealt: #{NumberFormat.large(damage)} damage!" if damage > 0
    end

    # How much gold this save has spent in shops.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.gold_spent
      gold = SaveStats[:gold_spent]
      "Has spent #{NumberFormat.grouped(gold)} gold in shops!" if gold > 0
    end

    # How many items this save has synthesized.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.items_synthesized
      count = SaveStats[:synthesize]
      "Has synthesized #{NumberFormat.counted(count, 'item')}!" if count > 0
    end

    # The deepest Labyrinth of Chaos floor reached.
    #
    # @return [String, nil] the line, nil when it does not apply
    def self.deepest_labyrinth_floor
      floor = $game_variables[NWConst::Var::EX_DUNGEON_REACH]
      "Has reached floor #{floor} in the Labyrinth of Chaos!" if floor > 0
    end

    # How much gold the party carries.
    #
    # @return [String] the line
    def self.gold_carried
      "Currently carrying #{NumberFormat.grouped($game_party.gold)} gold!"
    end
  end

  # Counters the game only keeps across all saves, kept per save.
  #
  # Stored in Discord/Stats instead of the save files, so saves load the same without the mod.
  module SaveStats
    # Folder inside the mod folder that holds one file per save.
    DIR = "Stats"

    # Every counter, in the order they are stored.
    KEYS = [:defeat, :escape, :lose, :synthesize, :gold_spent, :best_hit]

    @counts = {}

    # @param key [Symbol] the counter
    # @return [Integer] its value, 0 until it counted something
    def self.[](key)
      @counts[key] || 0
    end

    # @param key [Symbol] the counter
    # @param amount [Integer] what to add
    def self.add(key, amount)
      @counts[key] = self[key] + amount.to_i
    end

    # @param key [Symbol] the counter
    # @param value [Integer] a new candidate for the highest value
    def self.keep_highest(key, value)
      @counts[key] = [self[key], value.to_i].max
    end

    # Starts every counter over, for a new game.
    def self.reset
      @counts = {}
    end

    # Writes the counters for a save that was just written.
    #
    # @param index [Integer] the save slot
    def self.store(index)
      dir = MGQ_Discord.path(DIR)
      Dir.mkdir(dir) unless File.directory?(dir)

      body = "fingerprint=#{fingerprint}\n" + KEYS.map { |key| "#{key}=#{self[key]}\n" }.join
      File.open(file_for(index), "wb") { |file| file.write(body) }
    end

    # Reads the counters for a save that was just loaded.
    #
    # A file whose fingerprint does not match belongs to another playthrough and is ignored.
    #
    # @param index [Integer] the save slot
    def self.restore(index)
      reset
      path = file_for(index)
      return unless File.exist?(path)

      stored = {}
      File.open(path, "rb") { |file| file.read }.each_line do |line|
        key, value = line.chomp.split("=", 2)
        stored[key] = value if value
      end
      return unless stored["fingerprint"] == fingerprint

      KEYS.each { |key| @counts[key] = stored[key.to_s].to_i }
    end

    # Names the counter file of a save: Save/Save03.rvdata2 becomes Discord/Stats/Save03.txt.
    #
    # @param index [Integer] the save slot
    # @return [String] the full path
    def self.file_for(index)
      MGQ_Discord.path("#{DIR}/#{File.basename(DataManager.make_filename(index), '.rvdata2')}.txt")
    end

    # Ties a counter file to one exact save.
    #
    # A save replaced or copied outside the game gets another fingerprint, so its counters start over.
    #
    # @return [String] the fingerprint of the loaded save
    def self.fingerprint
      "#{$game_system.save_count}:#{$game_system.instance_variable_get(:@frames_on_save)}"
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
      fields["overworld"] = 1 if scene == "battle" && GameState.on_world_map?

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

# Saving, loading and starting a new game keep the per-save counters in step with the save slots.
begin
  module DataManager
    class << self
      alias mgq_discord_save_game_without_rescue save_game_without_rescue
      def save_game_without_rescue(index)
        result = mgq_discord_save_game_without_rescue(index)
        begin
          MGQ_Discord::SaveStats.store(index)
        rescue => e
          MGQ_Discord::Log.write("stats save failed: #{e.class}: #{e.message}")
        end
        result
      end

      alias mgq_discord_load_game_without_rescue load_game_without_rescue
      def load_game_without_rescue(index)
        result = mgq_discord_load_game_without_rescue(index)
        begin
          MGQ_Discord::SaveStats.restore(index)
        rescue => e
          MGQ_Discord::SaveStats.reset rescue nil
          MGQ_Discord::Log.write("stats load failed: #{e.class}: #{e.message}")
        end
        result
      end

      alias mgq_discord_setup_new_game setup_new_game
      def setup_new_game(*args)
        result = mgq_discord_setup_new_game(*args)
        MGQ_Discord::SaveStats.reset rescue nil
        result
      end
    end
  end
rescue => e
  MGQ_Discord::Log.write("save/load hooks FAILED: #{e.class}: #{e.message}")
end

# Autosaves bypass save_game_without_rescue. Without their own counter file, loading one would
# start every counter at 0.
begin
  module DataManager
    class << self
      alias mgq_discord_auto_save_game_without_rescue auto_save_game_without_rescue
      def auto_save_game_without_rescue(index)
        result = mgq_discord_auto_save_game_without_rescue(index)
        begin
          MGQ_Discord::SaveStats.store(index)
        rescue => e
          MGQ_Discord::Log.write("stats autosave failed: #{e.class}: #{e.message}")
        end
        result
      end
    end
  end
rescue => e
  MGQ_Discord::Log.write("autosave hook FAILED: #{e.class}: #{e.message}")
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
