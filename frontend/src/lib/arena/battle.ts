// A battle as the frontend sees it, and the rules for keeping that
// view current. Everything here is plain data and pure functions.
//
// The types mirror `Battles/Features/GetBattle` and
// `Battles/Features/WatchBattle` in the backend. The SignalR hub
// sends them as they are; the Kiota models are mapped onto them
// (`battles.ts`).

export type BattleStatus = 'Created' | 'WaitingForPlayers' | 'InProgress' | 'Completed';

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

/** Whether a monster can still act or be targeted. */
export function isStanding(combatant: Combatant): boolean {
	return combatant.hitPoints > 0;
}

/** How a battle ended, seen by one participant. */
export type Outcome = 'undecided' | 'won' | 'lost' | 'abandoned';

/**
 * How the battle ended for `participantId`. A completed battle
 * without a winner was abandoned: no human moved before a deadline.
 */
export function outcomeFor(battle: Battle, participantId: string): Outcome {
	if (battle.status !== 'Completed') {
		return 'undecided';
	}

	if (battle.winnerId === null) {
		return 'abandoned';
	}

	return battle.winnerId === participantId ? 'won' : 'lost';
}

/** Whole seconds until `deadline`, never negative; null without a deadline. */
export function secondsUntil(deadline: string | null, now: number): number | null {
	if (deadline === null) {
		return null;
	}

	return Math.max(0, Math.ceil((Date.parse(deadline) - now) / 1000));
}
