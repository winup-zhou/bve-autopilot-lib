// Main.cpp : プラグイン全体を統括します
//
// Copyright © 2019 Watanabe, Yuki
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA  02110 - 1301  USA

#include "stdafx.h"
#include "Main.h"
#include <algorithm>
#include <functional>
#include <iterator>
#include <limits>

namespace autopilot
{

    namespace
    {

        bool 全て押している(
            キー組合せ 実キー組合せ, キー組合せ 目標キー組合せ) noexcept
        {
            auto 押していないキー = ~実キー組合せ & 目標キー組合せ;
            return 押していないキー.none();
        }

        bool キー組合せ完成(
            キー組合せ 旧キー組合せ, キー組合せ 新キー組合せ,
            キー組合せ 目標キー組合せ) noexcept
        {
            return !全て押している(旧キー組合せ, 目標キー組合せ) &&
                全て押している(新キー組合せ, 目標キー組合せ);
        }

        制動指令 出力制動指令(
            手動制動自然数ノッチ 手動ノッチ, 自動制動自然数ノッチ 自動ノッチ,
            const 制動特性 &制動)
        {
            if (制動.非常ブレーキである(手動ノッチ)) {
                return 手動ノッチ; // 非常ブレーキは常に優先する
            }
            if (自動ノッチ == 自動制動自然数ノッチ{0}) {
                return 手動ノッチ;
            }

            // 念のためおかしな自動ノッチを出力しないようにしておく
            自動制動自然数ノッチ 自動最大ノッチ = 制動.自動最大ノッチ();
            if (自動ノッチ > 自動最大ノッチ) {
                自動ノッチ = 自動最大ノッチ;
            }

            mps2 手動 = 制動.減速度(手動ノッチ);
            mps2 自動 = 制動.減速度(自動ノッチ);
            if (手動 >= 自動) {
                return 手動ノッチ;
            }
            else {
                return 制動.指令(自動ノッチ);
            }
        }

        int 変換済入力力行ノッチ(const 共通状態 &状態) {
            int ノッチ = 状態.入力力行ノッチ();
            auto &変換表 = 状態.設定().ノッチ変換表();
            if (ノッチ <= 0 || 変換表.empty()) {
                return ノッチ;
            }

            auto ノッチインデックス =
                static_cast<std::vector<力行ノッチ>::size_type>(ノッチ) - 1;
            if (ノッチインデックス < 変換表.size()) {
                return 変換表[ノッチインデックス].value;
            }
            else {
                return 変換表.back().value;
            }
        }

    }

    Main::Main() :
        _状態{},
        _tasc{},
        _ato{},
        _tasc有効{true},
        _ato有効{true},
        _インチング状態{インチング状態::切},
        _通過済地上子{},
        _音声状態{}
    {
        _tasc.目標停止位置を監視([&](区間 位置のある範囲) {
            _ato.tasc目標停止位置変化(位置のある範囲);
        });
    }

    Main::~Main()
    {
        _tasc.目標停止位置を監視(nullptr);
    }

    void Main::リセット(int)
    {
        _状態.リセット();
        _tasc.リセット();
        _ato.リセット();
        _インチング状態 = インチング状態::切;
        _通過済地上子.clear();

        for (const auto &i : _状態.設定().音声割り当て()) {
            _音声状態[i.first].次に出力(ATS_SOUND_STOP);
        }
    }

    void Main::設定ファイル読込(LPCWSTR 設定ファイル名) {
        _状態.設定ファイル読込(設定ファイル名);

        _tasc有効 = false;
        _ato有効 = false;
        _ato.atc事前減速を設定(_状態.設定().atc事前減速());
    }

    void Main::逆転器操作(int ノッチ) noexcept
    {
        _状態.逆転器操作(ノッチ);
        _ato.逆転器操作(_状態);
    }

    void Main::力行操作(int ノッチ) noexcept
    {
        _状態.力行操作(ノッチ);
        _ato.力行操作(_状態);
    }

    void Main::制動操作(int ノッチ)
    {
        _状態.制動操作(ノッチ);
        _tasc.制動操作(_状態);
        _ato.制動操作(_状態);
    }

    void Main::キー押し(int キー)
    {
        auto 旧キー = _状態.押しているキー();
        _状態.キー押し(キー);
        auto 新キー = _状態.押しているキー();

        for (const auto &i : _状態.設定().キー割り当て()) {
            auto 目標キー = i.second;
            if (!キー組合せ完成(旧キー, 新キー, 目標キー)) {
                continue;
            }

            switch (i.first) {
            case キー操作::ato発進:
                if (_ato有効) {
                    _ato.発進(_状態, _tasc, ato::発進方式::手動);
                    _音声状態[音声::ato発進音].次に出力(ATS_SOUND_PLAY);
                }
                break;
            case キー操作::tascインチング:
                if (_tasc有効 && !_ato有効 && _tasc.インチング可能(_状態)) {
                    _インチング状態 = インチング状態::発進;
                    _音声状態[音声::インチング開始音].次に出力(ATS_SOUND_PLAY);
                }
                break;
            }
        }
    }

    void Main::地上子通過(const ATS_BEACONDATA &地上子)
    {
        // 地上子がどの位置に置かれているかは精確には分からず、前後の「経過」
        // の時の現在位置から推定する必要がある。
        // 次の「経過」が呼ばれるまで地上子を処理せずに溜めておく。
        _通過済地上子.push_back(地上子);
    }

    void Main::SetATOTASCStatus(int mode) {
        _tascato_mode = mode;
    }

    void Main::setATCLimit(double distance, int signalindex) {
		//_ato.atc事前減速を設定(true);
		_ato.ATC次闭塞信号現示変化(_状態.現在位置(), distance, signalindex, _状態);
    }

    void Main::setSignalLimit(double distance, double speed) {
		_ato.信号制限区間追加(_状態.現在位置(), distance, speed, _状態);
    }

    void Main::setSignalMaxDecel(double decel) {
        _状態.目安減速度設定(std::fabs(decel));
    }

    ATS_HANDLES Main::経過(
        const ATS_VEHICLESTATE &状態, int *出力値, int *音声状態)
    {
        m 直前位置 = _状態.現在位置();
        _状態.経過(状態);
        地上子通過執行(直前位置);
        
        // ATO 自動発進
        if (_ato有効 && _状態.自動発進可能な時刻である()) {
            _ato.発進(_状態, _tasc, ato::発進方式::自動);
        }

        // TASC インチング状態更新
        if (_インチング状態 == インチング状態::発進 && !_状態.停車中()) {
            _インチング状態 = インチング状態::走行;
        }
        else if (_インチング状態 == インチング状態::走行 && _状態.停車中()) {
            _インチング状態 = インチング状態::切;
        }

        _tasc.経過(_状態);
        _ato.経過(_状態, _tasc);

        // TASC と ATO の出力ノッチをまとめる
        自動制御指令 自動ノッチ = _状態.最大力行ノッチ();

        _ato有効 = _tascato_mode > 1;
        _tasc有効 = _tascato_mode > 0;
        if (_tascato_mode > 0)自動ノッチ = std::min(自動ノッチ, _tasc.出力ノッチ());
        if (_tascato_mode > 1)自動ノッチ = std::min(自動ノッチ, _ato.出力ノッチ());

        if (!ato有効() && !インチング中() ||
        //if ((出力値[73] == 0 || 出力値[92] == 7 || 出力値[137] == 1) && !インチング中() ||
            _状態.入力制動ノッチ() > 手動制動自然数ノッチ{0} ||
            _状態.入力逆転器ノッチ() <= 0)
        {
            自動ノッチ = std::min(自動ノッチ, 自動制御指令{力行ノッチ{0}});
        }

        if (ato一時停止中()) {
            自動ノッチ = 自動制御指令{};
        }

        ATS_HANDLES ハンドル位置{};
        ハンドル位置.Brake = 出力制動指令(
            _状態.入力制動ノッチ(), 自動ノッチ.制動成分(), _状態.制動()).value;
        if (_状態.入力力行ノッチ() >= 0) {
            ハンドル位置.Power = std::max(
                static_cast<int>(自動ノッチ.力行成分().value),
                変換済入力力行ノッチ(_状態));
        }
        else {
            ハンドル位置.Power = _状態.入力力行ノッチ();
        }
        ハンドル位置.Reverser = _状態.入力逆転器ノッチ();
        ハンドル位置.ConstantSpeed = ATS_CONSTANTSPEED_CONTINUE;

        _状態.出力(ハンドル位置, 自動ノッチ);

        for (auto &パネル出力 : _状態.設定().パネル出力対象登録簿()) {
            出力値[パネル出力.first] = パネル出力.second.出力(*this);
        }

        // 複数の音声が同じ音声出力先を共有している場合に互いに上書きしないように
        // ATS_SOUND_CONTINUE でないものだけ後で書き換える
        for (const auto &i : _状態.設定().音声割り当て()) {
            音声状態[i.second] = ATS_SOUND_CONTINUE;
        }
        for (const auto &i : _状態.設定().音声割り当て()) {
            auto 出力 = _音声状態[i.first].出力();
            if (出力 != ATS_SOUND_CONTINUE) {
                音声状態[i.second] = 出力;
            }
        }

        return ハンドル位置;
    }

    void Main::地上子通過執行(m 直前位置)
    {
        for (const ATS_BEACONDATA &地上子 : _通過済地上子) {
            _状態.地上子通過(地上子, 直前位置);
            _tasc.地上子通過(地上子, 直前位置, _状態);
            _ato.地上子通過(地上子, 直前位置, _状態);
        }
        _通過済地上子.clear();
        _通過済地上子.shrink_to_fit();
    }

}
