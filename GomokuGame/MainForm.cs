using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace GomokuGame
{
    public partial class MainForm : Form
    {
        private int over = 0;
        private int buttonOnClick = 0;
        private int btnUndoCount = 0;
        private const int STONE_SIZE = 40;
        private const int BOARD_SIZE = 10; // 10x10棋盘
        private int[,] boardState; // 0=空, 1=黑棋, 2=白棋
        private int currentPlayer = 1; // 当前玩家（1=黑棋先手）
        private int blackCount = 100; // 黑棋初始数量
        private int whiteCount = 100; // 白棋初始数量
        private Button[,] buttons = new Button[BOARD_SIZE, BOARD_SIZE];
        private Label lblBlack, lblWhite;
        private Color blackStoneColor = Color.Black;
        private Color whiteStoneColor = Color.White;
        private Color stoneBorderColor = Color.DarkGray;
        private enum Chess { none = 0, Black, White };
        private Chess mplayer = Chess.Black;//黑棋先手
        private Stack<(int Row, int Col, int Player)> moveHistory = new Stack<(int, int, int)>();

        public MainForm()
        {
            InitializeComponent();
            InitializeGame();
            this.DoubleBuffered = true;
        }

        private void InitializeGame()
        {
            // 创建棋盘布局
            TableLayoutPanel boardPanel = new TableLayoutPanel
            {
                RowCount = 120,
                ColumnCount = 120,
                Dock = DockStyle.Fill
            };

            // 初始化棋盘状态
            boardState = new int[BOARD_SIZE, BOARD_SIZE];

            // 创建100个Button控件
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    Button btn = new Button
                    {
                        Tag = new Point(row, col),
                        Size = new Size(50, 50),
                        Margin = new Padding(0),
                        Dock = DockStyle.Fill,
                        BackColor = Color.BurlyWood,
                        Font = new Font("Microsoft YaHei", 14, FontStyle.Bold)
                    };
                    // 移除按钮边框
                    btn.FlatAppearance.BorderSize = 0;

                    // 订阅绘制事件
                    btn.Paint += Button_Paint;

                    btn.Click += Button_Click;
                    buttons[row, col] = btn;

                    boardPanel.Controls.Add(btn, col, row);
                }
            }

            Button btnUndo = button2;
            btnUndo.Click += Button2_Click;

            // 创建计数显示区
            lblBlack = new Label { Text = $"黑棋: {blackCount}", AutoSize = true };
            lblWhite = new Label { Text = $"白棋: {whiteCount}", AutoSize = true };

            Label lblCurrentPlayer = new Label { Text = "黑方出棋", AutoSize = true };
            // 主布局
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 90));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            mainPanel.Controls.Add(boardPanel, 0, 0);

            FlowLayoutPanel countPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Controls = { lblBlack, lblWhite, lblCurrentPlayer }
            };
            mainPanel.Controls.Add(countPanel, 0, 1);

            this.Controls.Add(mainPanel);
        }
        private void Button_Paint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            Point pos = (Point)btn.Tag;
            int row = pos.X, col = pos.Y;
            int player = boardState[row, col];

            // 仅在有棋子时绘制棋子
            if (player != 0)
            {
                Rectangle stoneRect = new Rectangle(
                    (btn.Width - STONE_SIZE) / 2,
                    (btn.Height - STONE_SIZE) / 2,
                    STONE_SIZE,
                    STONE_SIZE
                );

                DrawStone(e.Graphics, stoneRect, player == 1);
            }
            else
            {
                // 撤销后恢复默认背景
                e.Graphics.Clear(Color.BurlyWood);
                // 重绘棋盘网格
                int centerX = btn.Width / 2;
                int centerY = btn.Height / 2;
                e.Graphics.FillEllipse(Brushes.Black, centerX - 2, centerY - 2, 4, 4);
            }
        }

        // 绘制棋子
        private void DrawStone(Graphics g, Rectangle rect, bool isBlack)
        {
            Color fillColor = blackStoneColor;

            if (!isBlack)
            {
                fillColor = whiteStoneColor;
                mplayer = Chess.Black;
            }
            else
            {
                fillColor = blackStoneColor;
                mplayer = Chess.White;
            }
            // 使用纯色填充而不是渐变
            using (var brush = new SolidBrush(fillColor))
            {
                g.FillEllipse(brush, rect);
            }

            // 绘制边框
            using (var pen = new Pen(stoneBorderColor, 1.5f))
            {
                g.DrawEllipse(pen, rect);
            }
        }
        private void reverseRole()//换手
        {
            if(over == 1)
            {
                mplayer = Chess.Black;
                UpdateCurrentPlayerText("黑方出棋");
                currentPlayer = 1;
                over = 0;
            }
            else if (mplayer == Chess.Black)
            {
                mplayer = Chess.White;
                UpdateCurrentPlayerText("你是白方，请走棋");
            }
            else if (mplayer == Chess.White)
            {
                mplayer = Chess.Black;
                UpdateCurrentPlayerText("你是黑方，请走棋");
            }
        }
        private void UpdateCurrentPlayerText(string text)
        {
            // 查找并更新显示当前玩家的Label
            foreach (Control control in this.Controls)
            {
                foreach (Control child in control.Controls)
                {
                    if (child is FlowLayoutPanel panel)
                    {
                        foreach (Control lbl in panel.Controls)
                        {
                            if (lbl is Label && text == "黑棋出棋") return;
                            else if (lbl is Label && !lbl.Text.StartsWith("黑棋:") && !lbl.Text.StartsWith("白棋:"))
                            {
                                lbl.Text = text;
                                return;
                            }
                        }
                    }
                }
            }
        }
        private void Button_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            Point pos = (Point)btn.Tag;
            int row = pos.X, col = pos.Y;
            

            // 禁止重复落子
            if (boardState[row, col] != 0) return;
            moveHistory.Push((row, col, currentPlayer));

            // 更新棋盘状态
            boardState[row, col] = currentPlayer;

            // 更新按钮显示
            btn.Invalidate();
            btn.Text = currentPlayer == 1 ? "黑" : "白";
            // 更新棋子计数
            if (currentPlayer == 1) blackCount--;
            else whiteCount--;
            UpdateCountDisplay();
            currentPlayer = 3 - currentPlayer; // 1->2, 2->1
            buttonOnClick = 1;
            // 检查胜利
            if (CheckWin(row, col))
            {
                MessageBox.Show($"{btn.Text}棋胜利！");
                ResetGame();
                return;
            }
            else
            {
                reverseRole();
            }
        }
        

        private bool CheckWin(int row, int col)
        {
            int player = boardState[row, col];
            int[] dx = { 1, 0, 1, 1 }; // 方向：↓ → ↘ ↙
            int[] dy = { 0, 1, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                int count = 1; // 当前落子已计数

                // 正向检查
                for (int step = 1; step < 5; step++)
                {
                    int r = row + dx[dir] * step;
                    int c = col + dy[dir] * step;
                    if (r < 0 || r >= BOARD_SIZE || c < 0 || c >= BOARD_SIZE) break;
                    if (boardState[r, c] == player) count++;
                    else break;
                }

                // 反向检查
                for (int step = 1; step < 5; step++)
                {
                    int r = row - dx[dir] * step;
                    int c = col - dy[dir] * step;
                    if (r < 0 || r >= BOARD_SIZE || c < 0 || c >= BOARD_SIZE) break;
                    if (boardState[r, c] == player) count++;
                    else break;
                }

                if (count >= 5) return true;
            }
            return false;
        }

        private void UpdateCountDisplay()
        {
            lblBlack.Text = $"黑棋: {blackCount}";
            lblWhite.Text = $"白棋: {whiteCount}";
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 设置初始玩家显示
            Label currentLabel = new Label { Text = "黑方出棋", AutoSize = true };
            
            countPanel.Controls.Add(currentLabel);
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            // 悔棋逻辑
            
            if (buttonOnClick == 1)
            {
                btnUndoCount = 0;
            }
            if (btnUndoCount == 1|| moveHistory.Count == 0)
            {
                MessageBox.Show("没有可悔棋的步骤！");
                return;
            }
            // 弹出最后一步操作（包含位置和玩家信息）
            var lastMove = moveHistory.Pop();
            int lastRow = lastMove.Row;
            int lastCol = lastMove.Col;
            int lastPlayer = lastMove.Player;

            // 恢复棋盘状态（清空棋子）
            boardState[lastRow, lastCol] = 0;

            // 更新按钮显示（清空文字并重绘）
            buttons[lastRow, lastCol].Text = "";
            buttons[lastRow, lastCol].BackColor = Color.BurlyWood;
            buttons[lastRow, lastCol].Invalidate(); // 触发重绘

            // 恢复棋子计数（根据悔棋的玩家类型增加对应数量）
            if (lastPlayer == 1)
                blackCount++;
            else
                whiteCount++;

            UpdateCountDisplay();

            // 切换回上一个玩家（悔棋后应由上一个玩家继续下棋）
            currentPlayer = lastPlayer;
            buttonOnClick = 0;
            btnUndoCount = 1;
            reverseRole();
        }
        private void ResetGame()
        {
            // 重置棋盘状态
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    boardState[i, j] = 0;
                    buttons[i, j].Text = "";
                }
            }
            blackCount = whiteCount = 100;
            currentPlayer = 1;
            UpdateCountDisplay();
            over = 1;
            reverseRole();
        }
    }
}