using UnityEngine;

namespace MyGame2.Stage
{
    // StageEvents를 구독하여 게임 이벤트 발생 시 자동으로 SFX를 재생한다.

    public sealed class StageSoundBridge : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private StageManager stageManager;

        [Header("BGM 설정")]
        [Tooltip("스테이지 인덱스가 홀수면 InPuzzle_2, 짝수면 InPuzzle_1")]
        [SerializeField] private bool alternateBGM = true;

        private void OnEnable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded        += OnStageLoaded;
            stageManager.Events.TurnExecuted        += OnTurnExecuted;
            stageManager.Events.StageClearTriggered += OnStageClear;
            stageManager.Events.GameOverTriggered   += OnGameOver;
            stageManager.Events.ActivePlayerChanged += OnActivePlayerChanged;
        }

        private void OnDisable()
        {
            if (stageManager == null) return;
            stageManager.Events.StageLoaded        -= OnStageLoaded;
            stageManager.Events.TurnExecuted        -= OnTurnExecuted;
            stageManager.Events.StageClearTriggered -= OnStageClear;
            stageManager.Events.GameOverTriggered   -= OnGameOver;
            stageManager.Events.ActivePlayerChanged -= OnActivePlayerChanged;
        }
        
        // 스테이지 로드

        private void OnStageLoaded(int stageIndex)
        {
            InGameSoundManager sm = InGameSoundManager.Instance;
            if (sm == null) return;

            sm.PlaySFX(sm.sfxGameEnter);

            if (alternateBGM)
            {
                SoundEntry bgm = (stageIndex % 2 == 0) ? sm.bgmInPuzzle1 : sm.bgmInPuzzle2;
                if (bgm.clip != null) sm.PlayBGM(bgm);
            }
            else
            {
                if (sm.bgmInPuzzle1.clip != null) sm.PlayBGM(sm.bgmInPuzzle1);
            }
        }
        
        // 턴 실행

        private void OnTurnExecuted(TurnOutcome outcome)
        {
            InGameSoundManager sm = InGameSoundManager.Instance;
            if (sm == null) return;

            StageState state = stageManager.CurrentState;
            if (state == null) return;
            if (state.IsUndoProcessing) return;
            if (!outcome.Executed) return;

            // 플레이어 이동 SFX
            if (outcome.PlayerMove.CanMove)
            {
                if (state.TryGetEntity(state.ActivePlayerId, out EntityState player)
                    && player.Has<PlayerData>()
                    && player.Get<PlayerData>().Slot == 2)
                    sm.PlaySFX(sm.sfxSlimeStep);
                else
                    sm.PlayFootStep();
            }

            // 상자 밀기 SFX (얼음 분기)
            if (outcome.PlayerMove.IsPushAndMove)
            {
                bool isIce = false;
                if (state.TryGetEntity(outcome.PlayerMove.TargetEntityId, out EntityState box)
                    && box.Has<BoxData>()
                    && box.Get<BoxData>().Ownership == BoxType.Ice)
                    isIce = true;

                sm.PlaySFX(isIce ? sm.sfxIcePush : sm.sfxObjectPush);
            }

            // 문 열기 SFX
            if (outcome.PlayerMove.Type == MoveResultType.OpenDoor)
                sm.PlaySFX(sm.sfxDoorOpen);

            // 텔레포트(벤트) SFX
            if (outcome.PlayerMove.CanMove)
            {
                GridPos from = outcome.PlayerMove.From;
                GridPos to = outcome.PlayerMove.To;
                int dist = Mathf.Abs(from.X - to.X) + Mathf.Abs(from.Y - to.Y);
                if (dist > 1)
                    sm.PlaySFX(sm.sfxVent);
            }

            // 발각 SFX
            if (outcome.CameraDetectedPlayerIds != null && outcome.CameraDetectedPlayerIds.Count > 0)
                sm.PlaySFX(sm.sfxDetect);

            // 부서지는 상자 SFX
            foreach (EntityState e in state.Entities)
            {
                if (!e.IsBox || !e.Has<BreakableData>()) continue;
                if (e.Get<BreakableData>().IsBreaking)
                {
                    sm.PlaySFX(sm.sfxBreakBox);
                    break;
                }
            }

            // 도착 칸: 버튼/레버/부쉬 SFX
            if (outcome.PlayerMove.CanMove)
                CheckLandedTile(sm, state, outcome.PlayerMove.To);
        }

        private void CheckLandedTile(InGameSoundManager sm, StageState state, GridPos pos)
        {
            foreach (EntityState e in state.Entities)
            {
                if (!e.IsAlive) continue;
                if (e.Position.X != pos.X || e.Position.Y != pos.Y) continue;

                if (e.Kind == EntityKind.ButtonEntity || e.Kind == EntityKind.LeverEntity)
                {
                    sm.PlaySFX(sm.sfxPressButton);
                    break;
                }
            }

            CellData cell = state.GetCell(pos);
            if (cell.HasBush)
                sm.PlaySFX(sm.sfxHideOnBush);
        }
        
        // 태그

        private void OnActivePlayerChanged(int newPlayerId)
        {
            InGameSoundManager sm = InGameSoundManager.Instance;
            if (sm == null) return;

            StageState state = stageManager.CurrentState;
            if (state == null || state.IsUndoProcessing) return;

            if (state.TryGetEntity(newPlayerId, out EntityState player) && player.Has<PlayerData>())
                sm.PlayTag(player.Get<PlayerData>().Slot == 2);
            else
                sm.PlaySFX(sm.sfxChrTag);
        }
        
        // 클리어 / 게임오버

        private void OnStageClear()
        {
            InGameSoundManager sm = InGameSoundManager.Instance;
            if (sm != null) sm.PlayGameClear();
        }

        private void OnGameOver()
        {
            InGameSoundManager sm = InGameSoundManager.Instance;
            if (sm != null) sm.PlaySFX(sm.sfxDamaged);
        }
    }
}