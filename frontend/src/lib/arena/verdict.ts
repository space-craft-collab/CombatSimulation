// What the result panel says once a battle is over, in the viewer's
// words: a player of either side, or a spectator. Pure.

import { outcomeFor, type Battle } from './battle.ts';

/** The result panel's heading and the line below it. */
export interface Verdict {
	title: string;
	detail: string;
}

/**
 * How a completed battle ended, as `viewerId` reads it.
 *
 * @param viewerId The viewer's side; null for a spectator.
 * @returns The verdict, or null while the battle is not over.
 */
export function verdictOf(battle: Battle, viewerId: string | null): Verdict | null {
	const detail = `Decided in round ${battle.round}.`;

	// A spectator takes no side: a decided battle is lost for them,
	// and named by its winner. No player id is empty.
	switch (outcomeFor(battle, viewerId ?? '')) {
		case 'undecided':
			return null;
		case 'unjoined':
			return {
				title: 'Nobody joined this battle.',
				detail: 'It waited for an opponent until its join deadline passed.'
			};
		case 'abandoned':
			return { title: 'The battle was abandoned.', detail: 'Nobody moved before the deadline.' };
		case 'won':
			return { title: 'You won!', detail };
		case 'lost': {
			const botWon = battle.combatants.some(
				(combatant) => combatant.participantId === battle.winnerId && combatant.isBot
			);

			return { title: botWon ? 'The bot won.' : `${battle.winnerId} won.`, detail };
		}
	}
}
