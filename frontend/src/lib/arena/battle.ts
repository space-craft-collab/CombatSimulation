// A battle as the frontend sees it, and the rules for keeping that
// view current. Everything here is plain data and pure functions.
//
// The types mirror `Battles/Features/GetBattle` and
// `Battles/Features/WatchBattle` in the backend. The SignalR hub
// sends them as they are; the Kiota models are mapped onto them
// (`battles.ts`).

export type BattleStatus = 'Created' | 'WaitingForPlayers' | 'InProgress' | 'Completed';

/** How hard a battle against the bot is: on Easy the bot's monsters enter weakened. */
export type Difficulty = 'Normal' | 'Easy';

/** The difficulties a visitor can choose from, the default first. */
export const DIFFICULTIES: readonly Difficulty[] = ['Normal', 'Easy'];

/** Reads a difficulty from text; anything that is none is the default. */
export function parseDifficulty(raw: string | null): Difficulty {
	return DIFFICULTIES.find((difficulty) => difficulty === raw) ?? DIFFICULTIES[0];
}

/** An effect that lies on a monster for some rounds. */
export type StatusEffect = 'Burn' | 'Guard' | 'Slow';

/** One ability of a monster in a battle and when it is ready. */
export interface AbilityCooldown {
	abilityId: string;
	/** Rounds until the monster can use it again; 0 when ready. */
	roundsLeft: number;
}

/** One effect on a monster in a battle. */
export interface ActiveEffect {
	kind: StatusEffect;
	/** Hit points per round for a burn, percent for a guard or a slow. */
	value: number;
	/** Rounds it still lasts, the current one included. */
	roundsLeft: number;
}

/** One monster on the field. */
export interface Combatant {
	slot: number;
	participantId: string;
	isBot: boolean;
	speciesId: string;
	hitPoints: number;
	maxHitPoints: number;
	/** The monster's abilities in their order; empty once the battle is over. */
	abilities: AbilityCooldown[];
	/** The effects on the monster; empty once the battle is over. */
	effects: ActiveEffect[];
}

/** A battle's full state. */
export interface Battle {
	battleId: string;
	arenaId: string;
	status: BattleStatus;
	/** The round being played; the last one played once the battle is completed. */
	round: number;
	/** The side left standing; null while undecided, and for an abandoned battle. */
	winnerId: string | null;
	/** ISO 8601; null when no turn is pending. */
	turnDeadline: string | null;
	combatants: Combatant[];
	/** Easy when the bot's monsters entered weakened; it stays so after the battle. */
	difficulty: Difficulty;
}

/** How an ability's element stood against its target's. */
export type Effectiveness = 'Weak' | 'Normal' | 'Strong';

/** What an ability does besides its damage. */
export type AbilityEffect = 'None' | StatusEffect | 'Heal';

/**
 * One thing that happened within a round: an ability that was used
 * and what it did, or a burn that took its toll.
 */
export interface RoundEvent {
	kind: 'Ability' | 'Burn';
	/** The monster that acted; for a burn, the burning monster itself. */
	actorSlot: number;
	/** The monster it happened to; the actor itself for a guard, a heal and a burn. */
	targetSlot: number;
	/** Null for a burn. */
	abilityId: string | null;
	damage: number;
	healing: number;
	targetHitPoints: number;
	effectiveness: Effectiveness;
	/** The effect that came into play; `None` for a plain hit. */
	effect: AbilityEffect;
}

/** A side's move for one round: one monster, one of its abilities, one target. */
export interface Move {
	actorSlot: number;
	/** The actor itself for an ability it uses on itself. */
	targetSlot: number;
	abilityId: string;
}

/** What one resolved round changed: the hub's `RoundResolved` message. */
export interface BattleEvent {
	battleId: string;
	/** The round that was played. */
	round: number;
	/** The battle's status, winner and deadline after that round. */
	status: BattleStatus;
	winnerId: string | null;
	turnDeadline: string | null;
	/** What happened, in order: for the log. */
	events: RoundEvent[];
	/** Every monster as it stands after the round. */
	combatants: Combatant[];
}

/**
 * How a round delta relates to a battle state:
 * - `next`: it is the round the state waits for, and can be applied;
 * - `old`: the state already contains it;
 * - `gap`: rounds in between are missing, the state must be reloaded.
 */
export type RoundRelation = 'next' | 'old' | 'gap';

/** Tells whether `event` can be applied to `battle`. */
export function relationOf(battle: Battle, event: BattleEvent): RoundRelation {
	if (battle.status === 'Completed' || event.round < battle.round) {
		return 'old';
	}

	return event.round === battle.round ? 'next' : 'gap';
}

/**
 * Applies the delta of the round `battle` waits for and returns the
 * new state; `battle` itself is not changed.
 */
export function applyRound(battle: Battle, event: BattleEvent): Battle {
	return {
		...battle,
		status: event.status,
		winnerId: event.winnerId,
		turnDeadline: event.turnDeadline,
		// A decided battle stays on its last round; otherwise the next one begins.
		round: event.status === 'Completed' ? event.round : event.round + 1,
		// The delta carries the monsters as they stand: hit points,
		// cooldowns and effects are not worked out a second time here.
		combatants: event.combatants
	};
}

/**
 * Picks the more advanced of two states of the same battle. A state
 * arrives over HTTP and over the hub, in no guaranteed order; one of
 * another battle is ignored.
 */
export function newerOf(current: Battle, candidate: Battle): Battle {
	if (candidate.battleId !== current.battleId) {
		return current;
	}

	return progressOf(candidate) >= progressOf(current) ? candidate : current;
}

function progressOf(battle: Battle): number {
	return battle.status === 'Completed' ? Number.POSITIVE_INFINITY : battle.round;
}

/**
 * The damage a monster took and the hit points it got back within
 * one round, all events together: a hit and a burn add up.
 */
export function changeOf(
	events: readonly RoundEvent[],
	slot: number
): { damage: number; healing: number } {
	return events
		.filter((event) => event.targetSlot === slot)
		.reduce(
			(total, event) => ({
				damage: total.damage + event.damage,
				healing: total.healing + event.healing
			}),
			{ damage: 0, healing: 0 }
		);
}

/** The sides of a battle, in the order of their first slot. */
export function sidesOf(battle: Battle): string[] {
	return [...new Set(battle.combatants.map((combatant) => combatant.participantId))];
}

/** Whether a monster can still act or be targeted. */
export function isStanding(combatant: Combatant): boolean {
	return combatant.hitPoints > 0;
}

/** How a battle ended, seen by one participant. */
export type Outcome = 'undecided' | 'won' | 'lost' | 'abandoned' | 'unjoined';

/**
 * How the battle ended for `participantId`. A completed battle with
 * one side only was opened and never joined before its join
 * deadline; one without a winner was abandoned: no human moved
 * before a turn deadline.
 */
export function outcomeFor(battle: Battle, participantId: string): Outcome {
	if (battle.status !== 'Completed') {
		return 'undecided';
	}

	return endingFor(sidesOf(battle), battle.winnerId, participantId);
}

/** How a completed battle can have ended for a participant. */
export type Ending = Exclude<Outcome, 'undecided'>;

/**
 * How a completed battle ended for `participantId`, told from its
 * sides and its winner alone: what a battle in a player's history
 * still has (`outcomeFor` has the rules).
 *
 * @param sides Every side of the battle, each once.
 */
export function endingFor(
	sides: readonly string[],
	winnerId: string | null,
	participantId: string
): Ending {
	if (sides.length < 2) {
		return 'unjoined';
	}

	if (winnerId === null) {
		return 'abandoned';
	}

	return winnerId === participantId ? 'won' : 'lost';
}

/** Whole seconds until `deadline`, never negative; null without a deadline. */
export function secondsUntil(deadline: string | null, now: number): number | null {
	if (deadline === null) {
		return null;
	}

	return Math.max(0, Math.ceil((Date.parse(deadline) - now) / 1000));
}

/** Whole seconds as a clock, `m:ss`: what is left of a join deadline. */
export function clockOf(seconds: number): string {
	const whole = Math.max(0, Math.floor(seconds));

	return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`;
}
