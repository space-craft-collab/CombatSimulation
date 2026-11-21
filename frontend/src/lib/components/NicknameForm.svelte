<script lang="ts">
	import { MAX_NICKNAME_LENGTH, validateNickname } from '#lib/arena/nickname.ts';
	import { player } from '#lib/arena/player.svelte.ts';

	interface Props {
		/** Called once a valid nickname is saved. */
		onsaved?: () => void;
	}

	let { onsaved }: Props = $props();

	let input = $state(player.nickname ?? '');
	let error = $state<string | null>(null);

	function save(event: SubmitEvent) {
		event.preventDefault();

		const result = validateNickname(input);

		if ('error' in result) {
			error = result.error;
			return;
		}

		error = null;
		player.choose(result.nickname);
		onsaved?.();
	}
</script>

<form class="flex flex-col gap-2" onsubmit={save} novalidate>
	<label class="text-sm font-medium" for="nickname">Nickname</label>
	<div class="flex gap-2">
		<input
			id="nickname"
			class="min-w-0 flex-1 rounded border border-slate-600 bg-slate-900 px-3 py-2"
			bind:value={input}
			maxlength={MAX_NICKNAME_LENGTH}
			autocomplete="nickname"
			aria-invalid={error !== null}
			aria-describedby="nickname-hint"
		/>
		<button class="rounded bg-amber-500 px-4 py-2 font-semibold text-slate-950" type="submit">
			Save
		</button>
	</div>
	<p id="nickname-hint" class="text-xs text-slate-400">
		Your battles and stats are kept under this name. There are no accounts yet, so it is not
		protected.
	</p>
	{#if error}
		<p class="text-sm text-red-400" role="alert">{error}</p>
	{/if}
</form>
