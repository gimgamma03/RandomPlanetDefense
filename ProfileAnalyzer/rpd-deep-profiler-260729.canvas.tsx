import { BarChart, Callout, Card, CardBody, CardHeader, Divider, Grid, H1, H2, H3, Row, Stack, Stat, Table, Text } from 'cursor/canvas';

const SOURCE = 'Unity Deep Profiler Marker Table · 260729RPDDeepmarkerTable.csv · ~2000 frames · Editor Play Mode';

const topSteady = [
  { label: 'Tower.Update', value: 63.7 },
  { label: 'PanelPlayerTMP.Update', value: 23.7 },
  { label: 'RotateObject.Update', value: 22.5 },
  { label: 'Projectile.Update', value: 12.6 },
  { label: 'TowerWeapon.AttackLoop', value: 10.0 },
  { label: 'GridHoverOverlay.LateUpdate', value: 7.6 },
  { label: 'TowerWeapon.SearchTarget', value: 7.2 },
  { label: 'ProjectileVfx.Update', value: 5.9 },
  { label: 'DamagePopup.PlayRoutine', value: 5.5 },
  { label: 'OrbitSatellitePivot.Update', value: 4.2 },
];

const categoryShare = [
  { label: 'Editor / Profiler overhead', value: 5331 + 774 + 734 },
  { label: 'PlayerLoop (all)', value: 2606 },
  { label: 'Camera / PlayMode render', value: 825 + 589 },
  { label: 'Scripts BehaviourUpdate', value: 379 },
  { label: 'Physics2D (simulate+cb)', value: 162 + 159 + 65 + 64 },
  { label: 'UGUI / Canvas', value: 154 + 149 },
  { label: 'GC.Collect (1 spike)', value: 229 },
];

export default function RpdDeepProfiler260729() {
  return (
    <Stack gap={20}>
      <Stack gap={6}>
        <H1>RPD Deep Profiler — 2026-07-29</H1>
        <Text tone="secondary" size="small">{SOURCE}</Text>
      </Stack>

      <Callout tone="warning" title="측정 환경 주의">
        Editor Play Mode Deep Profile입니다. EditorLoop(~2.7ms/frame), Profiler.Flush*(~0.75ms/frame),
        RenderPlayModeViewCameras(~0.41ms/frame)가 크게 잡힙니다. 포폴 수치 주장·최적화 효과는
        Development/Player 빌드에서 다시 재는 편이 안전합니다.
      </Callout>

      <Grid columns={4} gap={12}>
        <Stat value="792" label="Markers" />
        <Stat value="~2000" label="Frames in table" />
        <Stat value="~1.3 ms" label="PlayerLoop / frame (mean)" tone="info" />
        <Stat value="~0.19 ms" label="BehaviourUpdate / frame" tone="success" />
      </Grid>

      <H2>한 줄 결론</H2>
      <Text>
        평상시 스크립트 부하는 낮고, 눈에 띄는 문제는 (1) Physics2D 트리거 스파이크,
        (2) 타워 다수일 때 Tower.Update 호출량, (3) Editor/프로파일러 자체 오버헤드입니다.
        GC 229ms는 1회성(씬 로드/언로드 성격)으로 보입니다.
      </Text>

      <Grid columns={2} gap={16}>
        <Card>
          <CardHeader>Steady cost — 게임플레이 C# (Total Time ms)</CardHeader>
          <CardBody>
            <BarChart
              categories={topSteady.map((d) => d.label)}
              series={[{ name: 'Total Time (ms)', data: topSteady.map((d) => d.value) }]}
              horizontal
              height={320}
            />
            <Text tone="secondary" size="small">
              Source: Assembly-CSharp markers · Total Time over capture
            </Text>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>Capture mix — category totals (ms, overlapping)</CardHeader>
          <CardBody>
            <BarChart
              categories={categoryShare.map((d) => d.label)}
              series={[{ name: 'Sum of related markers (ms)', data: categoryShare.map((d) => d.value) }]}
              horizontal
              height={320}
            />
            <Text tone="secondary" size="small">
              Categories overlap (parent/child). Use for relative noise, not exact % of frame.
            </Text>
          </CardBody>
        </Card>
      </Grid>

      <H2>핫스팟 상세</H2>
      <Table
        headers={['Finding', 'Evidence', 'Priority']}
        rows={[
          [
            'Physics2D contact spike',
            'Physics2D.Simulate max ~57ms; Enemy.OnTriggerEnter2D mean 11.4ms / max 56.8ms (5 calls)',
            'High',
          ],
          [
            'Many Tower.Update calls',
            'Tower.Update total 63.7ms, 49,203 invokes over 639 frames (~77 calls/frame when active)',
            'Medium',
          ],
          [
            'UI / TMP churn',
            'PlayerUpdateCanvases ~0.08ms/fr; PanelPlayerTMP.Update 23.7ms total; TMP Layout Text spikes',
            'Low–Med',
          ],
          [
            'GC spike (one-shot)',
            'GC.Collect 229ms ×1 — aligns with load/unload style markers nearby',
            'Ignore for combat FPS',
          ],
          [
            'Editor overhead',
            'EditorLoop 5331ms total (~2.7ms/fr); Profiler flush ~0.75ms/fr',
            'Ignore for Player',
          ],
        ]}
        rowTone={['danger', 'warning', 'neutral', 'neutral', 'neutral']}
      />

      <H2>게임 코드 Top (Assembly-CSharp)</H2>
      <Table
        headers={['Marker', 'Total ms', 'Mean ms', 'Max ms', 'Count', 'Frames']}
        rows={[
          ['Tower.Update', '63.68', '0.100', '0.413', '49203', '639'],
          ['Enemy.OnTriggerEnter2D', '57.14', '11.427', '56.773', '5', '5'],
          ['PanelPlayerTMP.Update', '23.68', '0.037', '0.174', '639', '639'],
          ['RotateObject.Update', '22.46', '0.011', '0.078', '11029', '2000'],
          ['Projectile.Update', '12.60', '0.016', '0.105', '5601', '766'],
          ['TowerWeapon.AttackLoop', '9.96', '0.111', '0.447', '176', '90'],
          ['GridHoverOverlay.LateUpdate', '7.58', '0.012', '0.055', '639', '639'],
          ['TowerWeapon.SearchTarget', '7.21', '0.024', '0.302', '4038', '302'],
          ['ProjectileVfx.Update', '5.87', '0.008', '0.054', '5595', '765'],
          ['DamagePopup.PlayRoutine', '5.54', '0.026', '0.114', '892', '216'],
        ]}
      />

      <Divider />

      <H2>다음에 할 일 (포폴/최적화 관점)</H2>
      <Stack gap={8}>
        <H3>1. 재측정</H3>
        <Text>Development Player 빌드 + Deep Profile 끄고 Hierarchy/CPU Module만으로 전투 구간만 캡처.</Text>
        <H3>2. 스파이크 원인 확인</H3>
        <Text>
          Enemy.OnTriggerEnter2D / Physics2D trigger 폭주 — 골인·총알·슬로우 트리거가 한 프레임에
          몰리는지 Timeline에서 프레임 확인.
        </Text>
        <H3>3. 정상 프레임 후보</H3>
        <Text>
          Tower.Update / SearchTarget 호출 수 줄이기(이미 스태거 탐색 있음 — 타워 수 많을 때 체감).
          PanelPlayerTMP·RotateObject는 저비용이면 보류.
        </Text>
      </Stack>

      <Callout tone="info" title="포폴 문장용">
        “Deep Profile로 Editor 오버헤드와 런타임 스크립트를 분리해 보면, 전투 구간의 주 부하는
        Physics2D 콜백 스파이크와 타워 Update 호출량이다. 일반 BehaviourUpdate는 프레임당 ~0.2ms
        수준으로 낮았다.” — 수치 확정은 Player 빌드 재캡처 후.
      </Callout>
    </Stack>
  );
}
